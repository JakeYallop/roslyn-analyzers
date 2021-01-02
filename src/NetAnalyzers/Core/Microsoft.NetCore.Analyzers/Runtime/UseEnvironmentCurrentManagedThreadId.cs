// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Runtime
{
    /// <summary>CA1839: Use Environment.CurrentManagedThreadId.</summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class UseEnvironmentCurrentManagedThreadId : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1839";

        internal const string EnvironmentCurrentManagedThreadIdExpression = "Environment.CurrentManagedThreadId";
        internal const string ThreadCurrentThreadManagedThreadId = "Thread.CurrentThread.ManagedThreadId";

        internal static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorHelper.Create(RuleId,
            new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.UseEnvironmentPropertiesTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources), EnvironmentCurrentManagedThreadIdExpression),
            new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.UseEnvironmentPropertiesMessage), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources), EnvironmentCurrentManagedThreadIdExpression, ThreadCurrentThreadManagedThreadId),
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            description: null,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Retrieve thread and evironment types
                if (!compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingThread, out var threadType) ||
                    !compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemEnvironment, out var environmentType))
                {
                    return;
                }

                // Retrieve thread and environment symbols
                var threadMembers = threadType.GetMembers();
                var threadCurrentThreadSymbol = threadMembers.FirstOrDefault(m => m.Name == "CurrentThread");
                var threadManagedThreadIdSymbol = threadMembers.FirstOrDefault(m => m.Name == "ManagedThreadId");
                var environmentCurrentManagedThreadIdSybmol = environmentType.GetMembers("CurrentManagedThreadId").FirstOrDefault();
                if (threadCurrentThreadSymbol is null || threadManagedThreadIdSymbol is null || environmentCurrentManagedThreadIdSybmol is null)
                {
                    return;
                }

                compilationContext.RegisterOperationAction(operationContext =>
                {
                    // Warn if this is `Thread.CurrentThread.ManagedThreadId`
                    var maangedThreadIdPropertyReference = (IPropertyReferenceOperation)operationContext.Operation;
                    if (threadManagedThreadIdSymbol.Equals(maangedThreadIdPropertyReference.Property) &&
                        maangedThreadIdPropertyReference.Instance is IPropertyReferenceOperation managedThreadIdPropertyMember &&
                        threadCurrentThreadSymbol.Equals(managedThreadIdPropertyMember.Member))
                    {
                        operationContext.ReportDiagnostic(maangedThreadIdPropertyReference.CreateDiagnostic(Rule));
                    }
                }, OperationKind.PropertyReference);
            });
        }
    }
}