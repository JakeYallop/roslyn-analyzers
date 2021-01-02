// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Runtime
{
    /// <summary>CA1837: Use Environment.ProcessId</summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public sealed class UseEnvironmentCurrentManagedThreadIdFixer : CodeFixProvider
    {
        private const string EnvironmentCurrentManagedThreadIdExpression = "Environment.CurrentManagedThreadId";
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseEnvironmentCurrentManagedThreadId.RuleId);
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Document doc = context.Document;
            SemanticModel model = await doc.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxNode root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root.FindNode(context.Span, getInnermostNodeForTie: true) is SyntaxNode node &&
                model.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemEnvironment, out var environmentType) &&
                model.GetOperation(node, context.CancellationToken) is IPropertyReferenceOperation operation)
            {
                string title = string.Format(CultureInfo.InvariantCulture, MicrosoftNetCoreAnalyzersResources.UseEnvironmentPropertiesFix, EnvironmentCurrentManagedThreadIdExpression);
                context.RegisterCodeFix(
                    CodeAction.Create(title,
                    async cancellationToken =>
                    {
                        DocumentEditor editor = await DocumentEditor.CreateAsync(doc, cancellationToken).ConfigureAwait(false);
                        var replacement = editor.Generator.MemberAccessExpression(editor.Generator.TypeExpressionForStaticMemberAccess(environmentType), "CurrentManagedThreadId");
                        editor.ReplaceNode(node, replacement.WithTriviaFrom(node));
                        return editor.GetChangedDocument();
                    },
                    equivalenceKey: title),
                    context.Diagnostics);
            }
        }
    }
}