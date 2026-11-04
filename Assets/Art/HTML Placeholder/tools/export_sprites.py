"""Export Pawtisserie placeholder sprites from the HTML prototype.

Every sprite is drawn by the prototype's own Canvas 2D functions, so the PNGs match
what you see in the browser. Scale: 1 prototype unit = K px (default 4), so one tray
cell (50 units) = 200 px.  In Unity set Pixels Per Unit = 200 -> 1 cell = 1 world unit.

Usage:  python export_sprites.py <prototype.html> <out_dir> [--k 4] [--fonts <dir>]
"""

import argparse
import base64
import json
import pathlib

from playwright.sync_api import sync_playwright

# ---- the sprite list: name -> JS that draws it -------------------------------------
# Each entry: [folder/name, width_units, height_units, origin_x_units, origin_y_units, js_body]
# The JS body runs with (0,0) at the origin; all prototype globals (ctx, rr, ell, PASTRY ...) are in scope.
SPRITE_JS = r"""
(K) => {
  const out = {};
  const pivots = {};
  function snap(name, w, h, ox, oy, draw){
    canvas.width = Math.round(w*K); canvas.height = Math.round(h*K);
    ctx.setTransform(1,0,0,1,0,0); ctx.clearRect(0,0,canvas.width,canvas.height);
    ctx.setTransform(K,0,0,K,ox*K,oy*K);
    ctx.globalAlpha = 1; ctx.setLineDash([]); ctx.lineCap = 'butt';
    draw();
    out[name] = canvas.toDataURL('image/png');
    pivots[name] = [+(ox/w).toFixed(4), +(1 - oy/h).toFixed(4)];   // Unity pivot (0..1, y up)
  }
  now = 0;
  const KEYS = ['donut','macaron','cupcake','croissant'];
  const COL  = ['pink','blue','green','orange'];

  // pastries: 50x50 units = one tray cell, drawn at the tray size used in play (44)
  KEYS.forEach((k, i) => {
    snap(`pastries/${k}`,         50, 50, 25, 25, () => drawPastry(0, 0, 44, i));
    snap(`pastries/${k}_flat`,    50, 50, 25, 25, () => drawPastry(0, 0, 44, i, { flat:true }));
    snap(`pastries/${k}_ghost`,   50, 50, 25, 25, () => drawPastry(0, 0, 44, i, { flat:true, ghost:.22 }));
  });
  snap('pastries/shadow', 50, 50, 25, 25, () => ell(1, 16, 16, 4.5, 'rgba(74,44,42,.15)'));

  // obstacles
  snap('obstacles/cloche', 50, 56, 25, 31, () => cloche(0));
  KEYS.forEach((k, i) => snap(`obstacles/cloche_over_${k}`, 50, 56, 25, 31, () => drawPastry(0, 0, 44, i, { chain:true })));
  // same geometry as the prototype's drawCan, with the label fitted inside the can
  const canSprite = (primed) => () => {
    ell(1, 17, 13, 4, 'rgba(74,44,42,.15)');
    ctx.fillStyle = primed ? '#ffe17a' : '#fff6f0'; rr(-11, -8, 22, 25, 6); ctx.fill();
    ctx.fillStyle = '#ff7eb0'; rr(-11, -1, 22, 10, 2); ctx.fill();
    let px = 8; ctx.font = FONT(px); while (ctx.measureText('CREAM').width > 19 && px > 4){ px -= .25; ctx.font = FONT(px); }
    ctx.fillStyle = '#ffffff'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.fillText('CREAM', 0, 4.5);
    ctx.fillStyle = '#c94f82'; rr(-7, -14, 14, 7, 3); ctx.fill();
    ctx.fillStyle = '#ffffff'; ctx.beginPath();
    for (let i = 0; i < 10; i++){ const a = i/10*TAU - Math.PI/2, r = i % 2 ? 2 : 4.2; ctx.lineTo(Math.cos(a)*r, -18 + Math.sin(a)*r); }
    ctx.closePath(); ctx.fill();
    ctx.strokeStyle = 'rgba(255,255,255,.7)'; ctx.lineWidth = 2; ctx.lineCap = 'butt'; ctx.beginPath(); ctx.moveTo(-7, -5); ctx.lineTo(-7, 14); ctx.stroke();
  };
  snap('obstacles/cream_can',        50, 50, 25, 27, canSprite(false));
  snap('obstacles/cream_can_primed', 50, 50, 25, 27, canSprite(true));
  // danger ring used on overflowing pastries
  snap('fx/danger_ring', 50, 50, 25, 25, () => { ctx.strokeStyle = '#e5484d'; ctx.lineWidth = 3; ctx.beginPath(); ctx.arc(0, 0, 22, 0, TAU); ctx.stroke(); });

  // Butter the cat (drawn at s=60 -> native geometry)
  snap('butter/butter_focus', 96, 100, 44, 52, () => drawCat(0, 0, 60, 'focus', 0));
  snap('butter/butter_happy', 96, 100, 44, 52, () => drawCat(0, 0, 60, 'happy', 0));
  // paw: pad only, and pad + arm (arm runs straight up, 240 units)
  snap('butter/paw',     36, 34, 18, 18, () => { ctx.save(); ctx.beginPath(); ctx.rect(-18, -13, 36, 31); ctx.clip(); drawPaw(0, 0, 44, 0); ctx.restore(); });
  snap('butter/paw_arm', 36, 260, 18, 246, () => drawPaw(0, 0, 44, 0));
  // tail sweep: body at left, tip at right
  snap('butter/tail', 226, 60, 10, 12, () => { const g = gx; gx = 70; drawTail(200, 0, 1); gx = g; });

  // pastry boxes (104x56 body); origin = box body top-left
  COL.forEach((c, i) => {
    const mk = (closed, filled) => ({ color:i, oy:0, bounce:0, closed, need: 0, gift:null,
                                      spots: filled ? [1,1,1] : [0,0,0] });
    const B = (d) => () => { ctx.translate(-truckX(0) - 52, -TRUCK_Y - 14 - 28); drawBox(d, 0); };
    snap(`boxes/box_${KEYS[i]}_open`,   112, 80, 56, 46, B(mk(false, false)));
    snap(`boxes/box_${KEYS[i]}_open_slots`, 112, 80, 56, 46, B({ ...mk(false, false), need:3 }));
    snap(`boxes/box_${KEYS[i]}_full`,   112, 80, 56, 46, B(mk(false, true)));
    snap(`boxes/box_${KEYS[i]}_closed`, 112, 80, 56, 46, B(mk(true, true)));
    snap(`boxes/queue_box_${KEYS[i]}`,  40, 32, 20, 16, () => miniBox(0, 0, { color:i }, false, 1));
  });
  snap('boxes/queue_box_highlight', 40, 32, 20, 16, () => { ctx.strokeStyle = '#5b3a33'; ctx.lineWidth = 2; rr(-18, -13, 36, 27, 7); ctx.stroke(); });
  for (const t of ['pop','sweep','drill','box'])
    snap(`boxes/gift_tag_${{pop:'tap',sweep:'swipe',drill:'pounce',box:'flop'}[t]}`, 32, 34, 16, 15, () => drawGiftTag(0, 0, t, 1));
  snap('boxes/express_box', 172, 66, 106, 33, () => { const e = express; express = { x:0, y:0, bounce:0 }; drawExpress(); express = e; });

  // storage
  snap('storage/plate', 52, 32, 26, 12, () => {
    const SL = 48;
    ell(0, 10, SL/2, SL*.2, '#d9bcb0'); ell(0, 8, SL/2 - 1, SL*.19, '#fffaf4'); ell(0, 8, SL/2 - 7, SL*.11, '#f3e6e0');
  });
  snap('storage/cooling_rack', 80, 40, 40, 20, () => {
    const w = SOFT_CAP*SOFT + (SOFT_CAP-1)*6 + 12, h = 34, x = -w/2, y = -h/2;
    ctx.strokeStyle = RACK.dark; ctx.lineWidth = 3; rr(x, y, w, h, 5); ctx.stroke();
    ctx.strokeStyle = RACK.bar; ctx.lineWidth = 1.5;
    for (let k = 1; k < 6; k++){ const xx = x + k*w/6; ctx.beginPath(); ctx.moveTo(xx, y + 2); ctx.lineTo(xx, y + h - 2); ctx.stroke(); }
    for (let k = 1; k < 3; k++){ const yy = y + k*h/3; ctx.beginPath(); ctx.moveTo(x + 2, yy); ctx.lineTo(x + w - 2, yy); ctx.stroke(); }
  });
  snap('storage/cooling_rack_closed', 52, 12, 26, 6, () => { ctx.strokeStyle = RACK.dark; ctx.lineWidth = 3; rr(-22, -2.5, 44, 5, 5); ctx.stroke(); });
  snap('storage/stash_tin', 68, 56, 34, 28, () => {
    ctx.translate(-30, -26);
    const x = 0, w = 60, y = 0, h = 52;
    ctx.fillStyle = '#9fd3c7'; rr(x, y + 6, w, h - 6, 9); ctx.fill();
    ctx.strokeStyle = '#4a2c2a'; ctx.lineWidth = 2; rr(x, y + 6, w, h - 6, 9); ctx.stroke();
    ctx.fillStyle = '#6fb7a6'; rr(x - 2, y, w + 4, 9, 4); ctx.fill(); ctx.stroke();
    ctx.fillStyle = '#4a2c2a'; ctx.font = FONT(8); ctx.textAlign = 'center'; ctx.textBaseline = 'middle'; ctx.fillText('STASH', x + w/2, y + 4.5);
  });
  // the plate shelf for 5 plates (with plates), and the empty board
  const shelfBoard = (withPlates) => () => {
    const SL = 48, cap = 5, total = cap*SL + (cap-1)*6, bx = 0;
    ctx.fillStyle = '#ecd2c7'; rr(bx, 0, total + 16, 60, 12); ctx.fill();
    ctx.fillStyle = 'rgba(255,255,255,.45)'; rr(bx + 4, 3, total + 8, 6, 3); ctx.fill();
    if (withPlates) for (let i = 0; i < cap; i++){
      const x = 8 + i*(SL + 6) + SL/2, cy = 30;
      ell(x, cy + 10, SL/2, SL*.2, '#d9bcb0'); ell(x, cy + 8, SL/2 - 1, SL*.19, '#fffaf4'); ell(x, cy + 8, SL/2 - 7, SL*.11, '#f3e6e0');
    }
    stripes(bx + 10, 56, total - 4, 4, '#ff7eb0', '#fff3ea');
  };
  snap('storage/plate_shelf_5', 280, 62, 0, 1, shelfBoard(true));
  snap('storage/plate_shelf_board', 280, 62, 0, 1, shelfBoard(false));

  // tray
  snap('tray/tile_dark', 50, 50, 25, 25, () => { ctx.fillStyle = '#f1e1cb'; rr(-23, -23, 46, 46, 8); ctx.fill(); });
  snap('tray/tile_light', 50, 50, 25, 25, () => { ctx.fillStyle = '#f6ead8'; rr(-23, -23, 46, 46, 8); ctx.fill(); });
  snap('tray/tray_frame_3x3', 164, 164, 7, 7, () => {
    ctx.fillStyle = '#a99a92'; rr(-7, -7, 3*CELL + 14, 3*CELL + 14, 14); ctx.fill();
    ctx.fillStyle = '#c9bcb5'; rr(-3, -3, 3*CELL + 6, 3*CELL + 6, 11); ctx.fill();
  });
  for (const [t, nm] of [['pop','tap'],['sweep','swipe'],['drill','pounce'],['box','flop']])
    snap(`tray/aim_${nm}`, 50, 50, 25, 25, () => { ctx.globalAlpha = .45; ctx.fillStyle = CARDS[t].band; rr(-23, -23, 46, 46, 8); ctx.fill(); });
  snap('tray/aim_target', 50, 50, 25, 25, () => { ctx.strokeStyle = '#5b3a33'; ctx.lineWidth = 2.5; ctx.setLineDash([5,4]); rr(-23, -23, 46, 46, 8); ctx.stroke(); });
  snap('tray/tutorial_glow', 56, 56, 28, 28, () => { ctx.strokeStyle = 'rgb(246,196,83)'; ctx.lineWidth = 4; rr(-24, -24, 48, 48, 9); ctx.stroke(); });
  for (const k of ['truck','shelf','soft','over','crack','tnt']){
    const nm = { truck:'to_box', shelf:'to_plate', soft:'to_rack', over:'overflow', crack:'cloche_crack', tnt:'can' }[k];
    snap(`tray/badge_${nm}`, 22, 22, 11, 11, () => badge(0, 0, k));
  }

  // counter (top bar with 3 lanes) and queue strip, full prototype width
  snap('ui/counter_bg', 400, 94, 0, 0, () => {
    ctx.fillStyle = '#7a4e3c'; rr(0, 0, W, 94, 14); ctx.fill();
    ctx.fillStyle = '#8d5c47'; rr(0, 0, W, 10, 10); ctx.fill();
    ctx.strokeStyle = 'rgba(255,230,210,.35)'; ctx.lineWidth = 2; ctx.setLineDash([6,8]);
    for (const i of [1,2]){ ctx.beginPath(); ctx.moveTo(laneX(i)-2, 10); ctx.lineTo(laneX(i)-2, 84); ctx.stroke(); }
    ctx.setLineDash([]);
    stripes(0, 87, W, 7, '#ff7eb0', '#fff3ea');
  });
  snap('ui/queue_strip', 400, 34, 0, 0, () => { ctx.fillStyle = '#f0cfc4'; rr(0, 0, W, 34, 10); ctx.fill(); });
  snap('ui/awning_stripe_tile', 32, 8, 0, 0, () => stripes(0, 0, 32, 8, '#ff7eb0', '#fff3ea'));

  return { out, pivots };
}
"""

ICONS_JS = r"""
() => {
  // inline SVG icons from the page (boosters, restart, level-pill cat head, tutorial finger)
  const pick = (sel) => { const el = document.querySelector(sel); return el ? el.outerHTML : null; };
  const res = {};
  const btn = (id) => { const b = document.getElementById(id); return b ? b.querySelector('svg')?.outerHTML : null; };
  res['icon_shuffle'] = btn('bShuffle');
  res['icon_call_box'] = btn('bCall');
  res['icon_stash'] = btn('bStash');
  res['icon_finger'] = pick('#finger svg');
  const svgs = [...document.querySelectorAll('svg')].map(s => s.outerHTML);
  res['icon_butter_head'] = svgs.find(s => s.includes('#f2a65a') && s.includes('#fffaf4') && s.includes('circle'));
  res['icon_butter_head_alt'] = svgs.find(s => s.includes('#f2a65a') && s.includes('#ffe3d2'));
  res['icon_restart'] = svgs.find(s => s.includes('M3 3v5h5'));
  return res;
}
"""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("html")
    ap.add_argument("out")
    ap.add_argument("--k", type=float, default=4)
    ap.add_argument("--fonts", default=None, help="folder with fredoka-latin-*.woff2 / rubik-latin-*.woff2")
    a = ap.parse_args()
    out = pathlib.Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    K = a.k

    font_css = ""
    if a.fonts:
        f = pathlib.Path(a.fonts)
        for fam, w in [("Fredoka", 500), ("Fredoka", 600), ("Fredoka", 700), ("Rubik", 400), ("Rubik", 500), ("Rubik", 700)]:
            p = f / f"{fam.lower()}-latin-{w}-normal.woff2"
            if p.exists():
                b64 = base64.b64encode(p.read_bytes()).decode()
                font_css += f"@font-face{{font-family:'{fam}';font-weight:{w};src:url(data:font/woff2;base64,{b64}) format('woff2')}}\n"

    meta = {"units_to_px": K, "pixels_per_unit_for_1_cell": 50 * K, "sprites": {}}

    def save(name, data_url):
        p = out / f"{name}.png"
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(base64.b64decode(data_url.split(",", 1)[1]))
        return p

    with sync_playwright() as pw:
        br = pw.chromium.launch()
        pg = br.new_page(viewport={"width": 420, "height": 860}, device_scale_factor=K)
        pg.route("**/fonts.googleapis.com/**", lambda r: r.abort())
        pg.route("**/fonts.gstatic.com/**", lambda r: r.abort())
        # the prototype's code lives inside an IIFE, so inject the exporter into that scope
        src = pathlib.Path(a.html).read_text(encoding="utf-8")
        cut = src.rindex("})();")
        src = (src[:cut] + "window.__pawSprites = " + SPRITE_JS.strip() + ";\n"
               "window.__paw = { CARDS, patHTML };\n" + src[cut:])
        tmp = out / "_export_page.html"
        tmp.write_text(src, encoding="utf-8")
        pg.goto(tmp.resolve().as_uri())
        if font_css:
            pg.add_style_tag(content=font_css)
            pg.evaluate("""async () => { for (const w of [500,600,700]) await document.fonts.load(`${w} 12px Fredoka`);
                                         for (const w of [400,500,700]) await document.fonts.load(`${w} 12px Rubik`); }""")
        pg.wait_for_timeout(600)

        # 1) canvas sprites
        res = pg.evaluate('K => window.__pawSprites(K)', K)
        tmp.unlink()
        for name, url in res["out"].items():
            save(name, url)
            meta["sprites"][name] = {"pivot": res["pivots"][name]}

        # 2) hand cards (DOM elements), screenshotted flat at the same scale
        pg.evaluate("""() => { const st = document.createElement('style');
            st.textContent = '.bcard{translate:none!important;rotate:none!important;animation:none!important;box-shadow:none!important}';
            st.textContent += 'html,body{background:transparent!important} body>*:not(#exportHost){visibility:hidden!important}';
            document.head.appendChild(st); }""")
        names = {"pop": "tap", "sweep": "swipe", "drill": "pounce", "box": "flop"}
        for t, nm in names.items():
            pg.evaluate(
                """(t) => { const host = document.getElementById('exportHost') || Object.assign(document.body.appendChild(document.createElement('div')), {id:'exportHost'});
                    host.style.cssText = 'position:fixed;left:20px;top:20px;padding:0;background:transparent;z-index:9999';
                    const C = window.__paw.CARDS[t];
                    host.innerHTML = `<button class="bcard t-${t}" style="position:relative;left:0;bottom:auto">${window.__paw.patHTML(t)}<b>${C.name}</b><small>${C.hint}</small></button>`; }""",
                t)
            el = pg.locator("#exportHost .bcard")
            p = out / "cards" / f"card_{nm}.png"
            p.parent.mkdir(parents=True, exist_ok=True)
            el.screenshot(path=str(p), omit_background=True)
            meta["sprites"][f"cards/card_{nm}"] = {"pivot": [0.5, 0.5]}

        # 3) SVG icons rendered to 256 px PNGs
        icons = pg.evaluate(ICONS_JS)
        ip = br.new_page(viewport={"width": 256, "height": 256}, device_scale_factor=1)
        for nm, svg in icons.items():
            if not svg:
                continue
            svg = svg.replace("currentColor", "#4a2c2a")
            ip.set_content(f"<html><body style='margin:0;background:transparent'>"
                           f"<div style='width:256px;height:256px'>{svg.replace('<svg', '<svg width=256 height=256', 1)}</div></body></html>")
            p = out / "icons" / f"{nm}.png"
            p.parent.mkdir(parents=True, exist_ok=True)
            ip.locator("div").screenshot(path=str(p), omit_background=True)
            meta["sprites"][f"icons/{nm}"] = {"pivot": [0.5, 0.5]}
        br.close()

    # sizes
    from struct import unpack
    for name in meta["sprites"]:
        b = (out / f"{name}.png").read_bytes()
        w, h = unpack(">II", b[16:24])
        meta["sprites"][name]["size_px"] = [w, h]
    # 9-slice borders in px (left, bottom, right, top) for panels that stretch
    border = {"tray/tray_frame_3x3": 16, "storage/plate_shelf_board": 14, "ui/queue_strip": 10,
              "storage/stash_tin": 10, "tray/tile_light": 10, "tray/tile_dark": 10}
    manifest = {"unitsToPx": K, "pixelsPerUnit": 50 * K, "sprites": []}
    for name, m in sorted(meta["sprites"].items()):
        b = round(border.get(name, 0) * K)
        manifest["sprites"].append({"name": name, "pivotX": m["pivot"][0], "pivotY": m["pivot"][1],
                                    "width": m["size_px"][0], "height": m["size_px"][1],
                                    "borderL": b, "borderB": b, "borderR": b, "borderT": b})
    (out / "sprites.json").write_text(json.dumps(manifest, indent=2))
    print(len(meta["sprites"]), "sprites ->", out)


if __name__ == "__main__":
    main()
