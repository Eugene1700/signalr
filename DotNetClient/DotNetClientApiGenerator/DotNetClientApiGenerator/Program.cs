using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using SignalR.ApiGenerator.Abstract;
using Microsoft.Extensions.DependencyModel;


namespace DotNetClientApiGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var configPath = "defaultConfig.json";
            if (args.Length == 1)
            {
                configPath = args[0];
            }

            var configStr = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<GeneratorConfig>(configStr);

            var ns = config.TargetNamespace;
            var className = config.TargetClassName;
            var outPutFilePath = config.TargetFilePath;
            var typesSourceFile = config.TypeSourceCsPath;
            var assemblyPath = Path.GetFullPath(config.SourceAssemblyPath);
            if (!File.Exists(assemblyPath))
                throw new InvalidOperationException($"Assembly [{assemblyPath}] not found");
            var asl = new AssemblyLoader(Path.GetDirectoryName(assemblyPath));
            // var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            var assembly = asl.LoadFromAssemblyPath(assemblyPath);
            var hubType = assembly.GetTypes().FirstOrDefault(x => x.IsSubclassOf(typeof(Hub)));
            if (hubType == null)
                throw new InvalidOperationException($"Subtype of Hub not found");

            var definedTypes = new CodeTypeDeclaration[0];
            if (File.Exists(typesSourceFile))
                definedTypes = GetExisting(typesSourceFile, ns);
            
            var compileUnit = BuildApi(hubType, ns, className, definedTypes);
            GenerateSourceCode(compileUnit, outPutFilePath);
        }

        private static CodeTypeDeclaration[] GetExisting(string filepath, string ns)
        {
            var text = File.ReadAllText(filepath);
            var res = CSharpSyntaxTree.ParseText(text);
            var existingNamespace = res.GetRoot().DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>().SingleOrDefault(x => x.Name.ToString() == ns);
            var classes = existingNamespace?.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            var enums = existingNamespace?.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();
            var classesNames = classes?.Select(x => new CodeTypeDeclaration(x.Identifier.Text)).ToList();
            var enumsNames = enums?.Select(x => new CodeTypeDeclaration(x.Identifier.Text)).ToArray();
            classesNames?.AddRange(enumsNames);
            return classesNames?.ToArray();
        }

        private static void GenerateSourceCode(CodeCompileUnit compileUnit, string outputFilePath)
        {
            try
            {
                var provider = GetCodeDomProvider();

                StringWriter sw = new StringWriter();

                Console.WriteLine("Generating code...");
                provider.GenerateCodeFromCompileUnit(compileUnit, sw, null);

                string output = sw.ToString();

                Console.WriteLine("Dumping source...");
                Console.WriteLine(output);

                string backupPath = "";
                if (File.Exists(outputFilePath))
                {
                    Console.WriteLine("Backup");
                    backupPath = outputFilePath + ".bak";
                    File.Delete(backupPath);
                    File.Copy(outputFilePath, backupPath);
                    File.Delete(outputFilePath);
                }

                Console.WriteLine("Writing source to file...");
                Stream s = File.Open(outputFilePath, FileMode.OpenOrCreate);
                StreamWriter t = new StreamWriter(s);
                t.Write(output);
                t.Close();
                s.Close();
                if (!string.IsNullOrWhiteSpace(backupPath))
                    File.Delete(backupPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static CodeDomProvider GetCodeDomProvider()
        {
            string providerName = "cs";
            CodeDomProvider provider = CodeDomProvider.CreateProvider(providerName);
            return provider;
        }

        private static CodeCompileUnit BuildApi(Type type, string ns, string className,
            CodeTypeDeclaration[] declaredTypes)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            var resNamespace = new CodeNamespace(ns);
            compileUnit.Namespaces.Add(resNamespace);

            var resClass = new CodeTypeDeclaration(className);
            resNamespace.Types.Add(resClass);
            resClass.IsPartial = true;

            var hubConnectionProperty = resClass.GenerateProperty("HubConnection",
                new CodeTypeReference(typeof(HubConnection).UseReflectionType(resNamespace)),
                MemberAttributes.Family | MemberAttributes.Final);

            var receivedApi = type.GetMethods()
                .Where(x => x.GetCustomAttribute<ServerReceivedAttribute>() != null).Select(x =>
                {
                    if (x.ReturnType != typeof(Task))
                        throw new InvalidOperationException("Chathub methods return only async Task");
                    var res = GenerateMethodsSignature(x, true, resNamespace, declaredTypes);
                    var pars = x.GetParameters();

                    var invokeParams = new List<CodeExpression> {new CodePrimitiveExpression(x.Name)};
                    invokeParams.AddRange(pars.Select(p => new CodeVariableReferenceExpression(p.Name)));

                    var invokeExpr = new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression(hubConnectionProperty.Name),
                        "InvokeAsync",
                        invokeParams.ToArray()
                    );
                    invokeExpr.MarkCodeMethodInvokeExpressionAsAwait();
                    res.Statements.Add(invokeExpr);
                    return res;
                }).ToArray();

            resClass.Members.AddRange(receivedApi);

            var sendApiMethods = type.Assembly.GetTypes()
                .Single(x => x.GetCustomAttribute<ClientApiAttribute>() != null)
                .GetMethods(BindingFlags.Public | BindingFlags.Static).Select(x =>
                {
                    var pars = x.GetParameters().Where(p => p.ParameterType != typeof(IClientProxy))
                        .ToArray();
                    var res = new CodeMemberMethod
                    {
                        Name = x.Name.FirstLetterToUppercase() + "On",
                        Attributes = MemberAttributes.Family | MemberAttributes.Final,
                        ReturnType =
                            new CodeTypeReference(CodeDomHelper.UseReflectionType(typeof(IDisposable), resNamespace)),
                    };

                    var parsExprs = pars.GetCodeParameters(resNamespace, declaredTypes);

                    var typeParams = parsExprs.Select(p => p.Type).ToArray();
                    var handlerType = new CodeTypeReference("Action");
                    handlerType.TypeArguments.AddRange(typeParams);

                    var paramHandler = new CodeParameterDeclarationExpression(handlerType, "handler");
                    res.Parameters.Add(paramHandler);

                    var genericExpr = new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeVariableReferenceExpression(hubConnectionProperty.Name),
                            "On",
                            typeParams),
                        new CodePrimitiveExpression(x.Name),
                        new CodeVariableReferenceExpression(paramHandler.Name));
                    var returnStatement = new CodeMethodReturnStatement(genericExpr);
                    res.Statements.Add(returnStatement);
                    return res;
                }).ToArray();

            resClass.Members.AddRange(sendApiMethods);

            return compileUnit;
        }

        private static CodeMemberMethod GenerateMethodsSignature(MethodInfo x, bool isAsync, CodeNamespace ns,
            CodeTypeDeclaration[] declaredTypes)
        {
            var pars = x.GetParameters();
            var res = new CodeMemberMethod
            {
                Name = x.Name,
                Attributes = MemberAttributes.Family | MemberAttributes.Final,
                ReturnType = new CodeTypeReference(CodeDomHelper.UseReflectionType(typeof(Task), ns)),
            };
            if (isAsync)
                res.MarkCodeMemberMethodAsAsync();
            res.Parameters.AddRange(pars.GetCodeParameters(ns, declaredTypes));
            return res;
        }
    }
    
    public class AssemblyLoader : AssemblyLoadContext
    {
        private readonly string _folderPath;

        public AssemblyLoader(string folderPath)
        {
            _folderPath = folderPath;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var deps = DependencyContext.Default;
            var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
            if (res.Count > 0)
            {
                return Assembly.Load(new AssemblyName(res.First().Name));
            }
            else
            {
                var apiApplicationFileInfo = new FileInfo($"{_folderPath}{Path.DirectorySeparatorChar}{assemblyName.Name}.dll");
                if (File.Exists(apiApplicationFileInfo.FullName))
                {
                    var asl = new AssemblyLoader(apiApplicationFileInfo.DirectoryName);
                    return asl.LoadFromAssemblyPath(apiApplicationFileInfo.FullName);
                }
            }
            return Assembly.Load(assemblyName);
        }
    }
}