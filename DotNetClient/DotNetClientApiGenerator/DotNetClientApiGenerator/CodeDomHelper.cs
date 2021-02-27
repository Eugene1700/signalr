using System;
using System.CodeDom;
using System.Linq;
using System.Reflection;

namespace DotNetClientApiGenerator
{
    public static class CodeDomHelper
    {
        public static void MarkCodeMemberMethodAsAsync(this CodeMemberMethod method)
        {
            var returnTypeArgumentReferences = method.ReturnType.TypeArguments.OfType<CodeTypeReference>().ToArray();

            var asyncReturnType = new CodeTypeReference(String.Format("async {0}", method.ReturnType.BaseType),
                returnTypeArgumentReferences);
            method.ReturnType = asyncReturnType;
        }

        public static void MarkCodeMethodInvokeExpressionAsAwait(this CodeMethodInvokeExpression expression)
        {
            var variableExpression = expression.Method.TargetObject as CodeVariableReferenceExpression;
            if (variableExpression != null)
            {
                expression.Method.TargetObject =
                    new CodeVariableReferenceExpression(String.Format("await {0}", variableExpression.VariableName));
            }
        }

        public static CodeParameterDeclarationExpression[] GetCodeParameters(this ParameterInfo[] parameterInfos,
            CodeNamespace ns, CodeTypeDeclaration[] declaredTypes)
        {
            return parameterInfos
                .Select(p => BuildParameter(p, ns, declaredTypes)).ToArray();
        }

        private static CodeParameterDeclarationExpression BuildParameter(ParameterInfo p, CodeNamespace ns,
            CodeTypeDeclaration[] declaredTypes)
        {
            if (p.ParameterType.Namespace == "System")
                return new CodeParameterDeclarationExpression(p.ParameterType, p.Name);
            var builtType = BuildCodeTypeReference(p.ParameterType, ns, declaredTypes);
            return new CodeParameterDeclarationExpression(builtType, p.Name);
        }

        public static CodeTypeReference BuildCodeTypeReference(this Type type, CodeNamespace ns,
            CodeTypeDeclaration[] declaredTypes)
        {
            if (IsSystemType(type))
                return new CodeTypeReference(UseReflectionType(type, ns));

            if (type.IsGenericType)
                throw new InvalidOperationException("Generic types does not supported");

            if (type.IsArray)
                return BuildArrayCodeTypeReference(type, ns, declaredTypes);

            if (type.IsClass || type.IsInterface)
                return BuildClassCodeTypeReference(type, ns, declaredTypes);

            if (type.IsEnum)
                return BuildEnumCodeTypeReference(type, ns, declaredTypes);

            throw new InvalidOperationException($"{type.Name} does not supported");
        }

        private static CodeTypeReference BuildEnumCodeTypeReference(this Type type, CodeNamespace ns,
            CodeTypeDeclaration[] declaredTypes)
        {
            if (!type.IsEnum)
                throw new InvalidOperationException($"{type.Name} is not enum");

            var res = new CodeTypeDeclaration(type.Name)
            {
                IsEnum = true
            };

            if (TypeAlreadyWasDeclared(res, ns, declaredTypes))
                return new CodeTypeReference(res.Name);

            var enumValues = type.GetEnumNames().Select(x => new CodeMemberField(res.Name, x)).ToArray();
            res.Members.AddRange(enumValues);

            ns.Types.Add(res);
            return new CodeTypeReference(res.Name);
        }

        private static CodeTypeReference BuildClassCodeTypeReference(this Type type, CodeNamespace ns,
            CodeTypeDeclaration[] declaredTypes)
        {
            if (!type.IsClass && !type.IsInterface)
                throw new InvalidOperationException($"{type.Name} is not class or interface");

            var res = new CodeTypeDeclaration(type.Name)
            {
                IsClass = type.IsClass, IsInterface = type.IsInterface
            };

            if (TypeAlreadyWasDeclared(res, ns, declaredTypes))
                return new CodeTypeReference(res.Name);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToArray())
            {
                res.GenerateProperty(prop.Name, BuildCodeTypeReference(prop.PropertyType, ns, declaredTypes),
                    MemberAttributes.Public);
            }

            ns.Types.Add(res);
            return new CodeTypeReference(res.Name);
        }

        private static bool TypeAlreadyWasDeclared(this CodeTypeDeclaration res, CodeNamespace ns,
            CodeTypeDeclaration[] declaredTypes)
        {
            if (declaredTypes.Any(x => x.Name == res.Name))
                return true;

            for (int i = 0; i < ns.Types.Count; i++)
            {
                if (ns.Types[i].Name == res.Name)
                    return true;
            }

            return false;
        }

        private static CodeTypeReference BuildArrayCodeTypeReference(this Type type, CodeNamespace ns,
            CodeTypeDeclaration[] declaredTypes)
        {
            if (!type.IsArray)
                throw new InvalidOperationException($"{type.Name} is not array");

            var elementType = type.GetElementType();

            var elementTypeRef = elementType.BuildCodeTypeReference(ns, declaredTypes);
            var codeTypeDeclaration = new CodeTypeDeclaration(type.Name)
            {
                IsClass = type.IsClass
            };
            return new CodeTypeReference(codeTypeDeclaration.Name);
        }

        private static bool IsSystemType(this Type type)
        {
            return type.Namespace == "System";
        }

        public static Type UseReflectionType(this Type type, CodeNamespace root)
        {
            for (int i = 0; i < root.Imports.Count; i++)
            {
                if (root.Imports[i].Namespace == type.Namespace)
                    return type;
            }

            root.Imports.Add(new CodeNamespaceImport(type.Namespace));
            return type;
        }

        public static CodeMemberProperty GenerateProperty(this CodeTypeDeclaration targetClass, string propertyName,
            CodeTypeReference targetTypeReference, MemberAttributes attributes)
        {
            var targetField = new CodeMemberField
            {
                Name = "_" + propertyName.FirstLetterToLowercase(),
                Type = targetTypeReference,
                Attributes = MemberAttributes.Private
            };
            targetClass.Members.Add(targetField);

            var targetProperty = new CodeMemberProperty
            {
                HasGet = true,
                HasSet = true,
                Name = propertyName.FirstLetterToUppercase(),
                Type = targetTypeReference,
                Attributes = attributes
            };
            targetProperty.GetStatements.Add(new CodeMethodReturnStatement(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), targetField.Name)));
            targetProperty.SetStatements.Add(new CodeAssignStatement(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), targetField.Name),
                new CodePropertySetValueReferenceExpression()));
            targetClass.Members.Add(targetProperty);
            return targetProperty;
        }
    }
}