using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RoslynTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var sln = "";
            var logPath = "";
            try
            {
                sln = args[0];
                logPath = args[1];
            }
            catch
            {
                sln = @"C:\Users\Ibragim\source\repos\Disser2\Disser.sln";
                logPath = @"..\..\LoggingAPI\bin\Debug\netcoreapp2.0\LoggingAPI.dll";
            }
            UpdateProjects(sln, logPath);
            UpdateFiles(sln);
        }

        static void UpdateProjects(string soluiton, string assemblyPath)
        {
            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(soluiton).Result;
            var rewrittenSolution = solution;
            var projectIds = rewrittenSolution.ProjectIds;
            for (int i = 0; i < projectIds.Count; i++)
            {
                var addedRef = MetadataReference.CreateFromFile(assemblyPath)
                        .WithProperties(new MetadataReferenceProperties(MetadataImageKind.Assembly));

                rewrittenSolution = rewrittenSolution.AddMetadataReference(projectIds[i], addedRef);
                workspace.TryApplyChanges(rewrittenSolution);
            }
        }

        static void UpdateFiles(string soluiton)
        {
            var dir = new FileInfo(soluiton).Directory.FullName;
            var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).ToList();
            files = files.FindAll(_ => !_.Contains("AssemblyInfo"));

            foreach (var file in files)
            {
                var workspace = MSBuildWorkspace.Create();
                SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
                var root = (CompilationUnitSyntax)tree.GetRoot();

                NameSyntax name = IdentifierName(" LoggingAPI");
                root = root.AddUsings(UsingDirective(name));
                List<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

                var count = methods.Count;
                for (int i = 0; i < count; i++)
                {
                    methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
                    if (methods[i].ParameterList.Parameters.Count == 0)
                        continue;
                    var parameters = new List<string>();
                    string body = "";
                    foreach (var parameter in methods[i].ParameterList.Parameters)
                    {
                        var parameterName = parameter.Identifier.Text;
                        body += parameterName + "={" + parameterName.ToString() + "} ";
                    }
                    body = "Logging.LogToConsole($\"" + body + "\");\r\n";
                    var list = new List<StatementSyntax>();
                    StatementSyntax myStatement = ParseStatement(body);
                    list.Add(myStatement);
                    try
                    {
                        var newBody = methods[i].Body.WithStatements(methods[i].Body.Statements.InsertRange(0, list));
                        var newMethod = methods[i].WithBody(newBody);
                        root = root.ReplaceNode(methods[i], Formatter.Format(newMethod, workspace));
                    }
                    catch
                    {
                        Console.WriteLine($"Method {methods[i].Identifier} is empty of file {new FileInfo(file).Name}");
                        continue;
                    }

                }
                var root2 = Formatter.Format(root, workspace);
                File.WriteAllText(file, root2.ToString());
                Console.WriteLine("Ready! " + new FileInfo(file).Name);
            }
            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}