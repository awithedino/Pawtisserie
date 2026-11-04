---
paths:
  - "**/Tests/**"
  - "Tools/**"
---
# Tests and validators

- Game rules are tested in EditMode against the pure-C# sim (`Paw.Sim`). PlayMode only for things that need frames or scenes.
- Determinism is a tested property: the same level id + seed + list of (card, tile) must give the same outcome every time (GDD §18.4). Add a replay test with every rule change.
- **Fast loop:** `cd Tools/SimHarness && dotnet run -c Release [-- NameFilter]` compiles the real `Scripts/Sim`, `Scripts/Presentation`, `Scripts/Runtime/Config`, `Scripts/Runtime/Services` sources and every EditMode test against small UnityEngine stubs and runs them in seconds. Use it after every change to `Scripts/Sim` or `Scripts/Presentation`; run the Unity Test Runner before reporting done. Test classes must be public with a parameterless constructor and **no `[SetUp]`/`[TearDown]`** (the harness only runs `[Test]`/`[TestCase]`); put setup in a helper called from each test. If Unity ignores a brand-new test file (seen on Sand Drop), append the class to an existing test file.
- **Compile check:** `cd Tools/CompileCheck && dotnet build -c Release` compiles every game script with Roslyn against the installed Unity assemblies in a few seconds and reports compile errors without the Editor. Run it before asking Unity to recompile. It builds nothing runnable and needs one Editor-built `Library` folder and the Unity version in its csproj.
- The harness uses NuGet NUnit 3 (restore needs network once); Unity 2022.3 runs NUnit 2.6.4. Keep tests on the common API (`Assert.AreEqual/IsTrue/IsFalse/IsNotNull/Throws`, `[Test]`, `[TestCase]`).
- A new file under `Scripts/Runtime/Config` or `Scripts/Runtime/Services` may use only the Unity API that `Tools/SimHarness/UnityStubs.cs` fakes; add the stub, or move the file out of those folders.
- Never disable or weaken a test to make it pass.
- Unity CLI: `unity test . --mode EditMode --output ./test-results.xml --timeout 600` (project must not be open in another Editor); with an Editor open use MCP or the Test Runner.
