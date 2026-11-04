using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

static class Program
{
    static int Main(string[] args)
    {
        string filter = args.Length > 0 ? args[0] : null;
        int pass = 0, fail = 0;
        foreach (var type in typeof(Program).Assembly.GetTypes().Where(t => t.IsClass && t.IsPublic).OrderBy(t => t.Name))
        {
            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var cases = m.GetCustomAttributes<TestCaseAttribute>().Select(a => a.Arguments).ToList();
                bool isTest = m.GetCustomAttribute<TestAttribute>() != null;
                if (!isTest && cases.Count == 0) continue;
                if (cases.Count == 0) cases.Add(new object[0]);
                foreach (var a in cases)
                {
                    string name = type.Name + "." + m.Name + (a.Length > 0 ? "(" + string.Join(",", a) + ")" : "");
                    if (filter != null && !name.Contains(filter)) continue;
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        var inst = Activator.CreateInstance(type);
                        m.Invoke(inst, a);
                        pass++; Console.WriteLine("PASS " + name + " (" + sw.Elapsed.TotalSeconds.ToString("F1") + "s)");
                    }
                    catch (Exception ex)
                    {
                        fail++;
                        var inner = ex is TargetInvocationException t ? t.InnerException : ex;
                        Console.WriteLine("FAIL " + name + "\n     " + inner.GetType().Name + ": " + inner.Message.Replace("\n", "\n     "));
                    }
                }
            }
        }
        Console.WriteLine($"== {pass} passed, {fail} failed");
        return fail == 0 ? 0 : 1;
    }
}
