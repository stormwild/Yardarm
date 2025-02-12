﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.OpenApi.Models;
using Yardarm.Generation.MediaType;
using Yardarm.Generation.Request.Internal;
using Yardarm.Names;
using Yardarm.Serialization;
using Yardarm.Spec;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Yardarm.Generation.Request
{
    public class RequestTypeGenerator : TypeGeneratorBase<OpenApiOperation>
    {
        protected IMediaTypeSelector MediaTypeSelector { get; }
        protected IList<IRequestMemberGenerator> MemberGenerators { get; }
        protected ISerializerSelector SerializerSelector { get; }

        protected IRequestsNamespace RequestsNamespace { get; }

        protected OpenApiOperation Operation => Element.Element;

        public RequestTypeGenerator(ILocatedOpenApiElement<OpenApiOperation> operationElement,
            GenerationContext context, IMediaTypeSelector mediaTypeSelector,
            IList<IRequestMemberGenerator> memberGenerators,
            IRequestsNamespace requestsNamespace, ISerializerSelector serializerSelector)
            : base(operationElement, context, null)
        {
            MediaTypeSelector = mediaTypeSelector ?? throw new ArgumentNullException(nameof(mediaTypeSelector));
            MemberGenerators = memberGenerators ?? throw new ArgumentNullException(nameof(memberGenerators));
            RequestsNamespace = requestsNamespace ?? throw new ArgumentNullException(nameof(requestsNamespace));
            SerializerSelector = serializerSelector ?? throw new ArgumentNullException(nameof(serializerSelector));
        }

        protected override YardarmTypeInfo GetTypeInfo()
        {
            INameFormatter formatter = Context.NameFormatterSelector.GetFormatter(NameKind.Class);
            NameSyntax ns = Context.NamespaceProvider.GetNamespace(Element);

            return new YardarmTypeInfo(QualifiedName(ns,
                IdentifierName(formatter.Format(Operation.OperationId + "Request"))));
        }

        public override QualifiedNameSyntax GetChildName<TChild>(ILocatedOpenApiElement<TChild> child,
            NameKind nameKind) =>
            QualifiedName((NameSyntax)TypeInfo.Name, IdentifierName(
                Context.NameFormatterSelector.GetFormatter(nameKind).Format(child.Key + "-Model")));

        public override IEnumerable<MemberDeclarationSyntax> Generate()
        {
            string className = ((QualifiedNameSyntax)TypeInfo.Name).Right.Identifier.ValueText;

            ClassDeclarationSyntax declaration = ClassDeclaration(className)
                .AddElementAnnotation(Element, Context.ElementRegistry)
                .AddGeneratorAnnotation(this)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SimpleBaseType(RequestsNamespace.OperationRequest));

            declaration = declaration.AddMembers(
                GenerateParameterProperties(className)
                    .Concat(MemberGenerators
                        .SelectMany(p => p.Generate(Element, null)))
                    .ToArray());

            yield return declaration;

            if (Element.GetRequestBody()?.GetMediaTypes().Any(p => SerializerSelector.Select(p) == null) ?? false)
            {
                var buildContentMethod = declaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .First(p => p.Identifier.Text == BuildContentMethodGenerator.BuildContentMethodName);

                var httpContentGenerator =
                    new HttpContentRequestTypeGenerator(Element, Context, this, buildContentMethod);

                foreach (var otherMember in httpContentGenerator.Generate())
                {
                    yield return otherMember;
                }
            }
        }

        protected virtual IEnumerable<MemberDeclarationSyntax> GenerateParameterProperties(string className)
        {
            foreach (var parameter in Element.GetParameters())
            {
                var schema = parameter.GetSchemaOrDefault();

                yield return CreatePropertyDeclaration(parameter, schema, className);

                if (parameter.Element.Reference == null && schema.Element.Reference == null)
                {
                    foreach (var member in Context.TypeGeneratorRegistry.Get(schema).Generate())
                    {
                        yield return member;
                    }
                }
            }
        }

        protected virtual PropertyDeclarationSyntax CreatePropertyDeclaration(ILocatedOpenApiElement<OpenApiParameter> parameter,
            ILocatedOpenApiElement<OpenApiSchema> schema, string className)
        {
            string propertyName = Context.NameFormatterSelector.GetFormatter(NameKind.Property).Format(parameter.Key);

            if (propertyName == className)
            {
                propertyName += "Value";
            }

            var typeName = Context.TypeGeneratorRegistry.Get(schema).TypeInfo.Name;

            var propertyDeclaration = PropertyDeclaration(typeName, propertyName)
                .AddElementAnnotation(parameter, Context.ElementRegistry)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

            return propertyDeclaration;
        }
    }
}
