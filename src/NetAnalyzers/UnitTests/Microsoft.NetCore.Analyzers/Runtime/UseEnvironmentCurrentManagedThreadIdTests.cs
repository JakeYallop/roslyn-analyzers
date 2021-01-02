// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Runtime.UseEnvironmentCurrentManagedThreadId,
    Microsoft.NetCore.Analyzers.Runtime.UseEnvironmentCurrentManagedThreadIdFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Runtime.UseEnvironmentCurrentManagedThreadId,
    Microsoft.NetCore.Analyzers.Runtime.UseEnvironmentCurrentManagedThreadIdFixer>;
namespace Microsoft.NetCore.Analyzers.Runtime.UnitTests
{
    public class UseEnvironmentCurrentManagedThreadIdTests
    {
        [Fact]
        public async Task NoDiagnostics_CSharp()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading;

namespace System
{
    public static class Environment
    {
        public static int CurrentManagedThreadId => 0;
    }
}

class C
{
    void M()
    {
        int id = C.CurrentThread.ManagedThreadId;
    }

    private static C CurrentThread => new C();
    public int ManagedThreadId => 0;
}
");
        }

        [Fact]
        public async Task Diagnostics_FixApplies_CSharp()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;
using System.Threading;

namespace System
{
    public static class Environment
    {
        public static int CurrentManagedThreadId => 0;
    }
}

class C
{
    int M()
    {
        int pid = [|Thread.CurrentThread.ManagedThreadId|];
        pid = [|Thread.CurrentThread/*somecommentinbetweenpropertyaccess*/.ManagedThreadId|];
        Use([|Thread.CurrentThread.ManagedThreadId|]);
        Use(""test"",
            [|Thread.CurrentThread.ManagedThreadId|]);
        Use(""test"",
            [|Thread.CurrentThread.ManagedThreadId|] /* comment */,
            0.0);
        return [|Thread.CurrentThread.ManagedThreadId|];
    }

    void Use(int pid) {}
    void Use(string something, int pid) {}
    void Use(string something, int pid, double somethingElse) { }
}
",
@"
using System;
using System.Threading;

namespace System
{
    public static class Environment
    {
        public static int CurrentManagedThreadId => 0;
    }
}

class C
{
    int M()
    {
        int pid = Environment.CurrentManagedThreadId;
        pid = Environment.CurrentManagedThreadId;
        Use(Environment.CurrentManagedThreadId);
        Use(""test"",
            Environment.CurrentManagedThreadId);
        Use(""test"",
            Environment.CurrentManagedThreadId /* comment */,
            0.0);
        return Environment.CurrentManagedThreadId;
    }

    void Use(int pid) {}
    void Use(string something, int pid) {}
    void Use(string something, int pid, double somethingElse) { }
}
");
        }

        [Fact]
        public async Task Diagnostics_FixApplies_VB()
        {
            await VerifyVB.VerifyCodeFixAsync(
@"
Imports System
Imports System.Threading

Namespace System
    Class Environment
        Public Shared ReadOnly Property CurrentManagedThreadId As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace

Class C
    Private Function M() As Integer
        Dim pid As Integer = [|Thread.CurrentThread.ManagedThreadId|]
        pid = [|Thread.CurrentThread.ManagedThreadId|]
        Use([|Thread.CurrentThread.ManagedThreadId|])
        Use("", test, "", [|Thread.CurrentThread.ManagedThreadId|])
        Use("", test, "", [|Thread.CurrentThread.ManagedThreadId|], 0.0)
        Return [|Thread.CurrentThread.ManagedThreadId|]
    End Function

    Private Sub Use(ByVal pid As Integer)
    End Sub

    Private Sub Use(ByVal something As String, ByVal pid As Integer)
    End Sub

    Private Sub Use(ByVal something As String, ByVal pid As Integer, ByVal somethingElse As Double)
    End Sub
End Class
",
@"
Imports System
Imports System.Threading

Namespace System
    Class Environment
        Public Shared ReadOnly Property CurrentManagedThreadId As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace

Class C
    Private Function M() As Integer
        Dim pid As Integer = Environment.CurrentManagedThreadId
        pid = Environment.CurrentManagedThreadId
        Use(Environment.CurrentManagedThreadId)
        Use("", test, "", Environment.CurrentManagedThreadId)
        Use("", test, "", Environment.CurrentManagedThreadId, 0.0)
        Return Environment.CurrentManagedThreadId
    End Function

    Private Sub Use(ByVal pid As Integer)
    End Sub

    Private Sub Use(ByVal something As String, ByVal pid As Integer)
    End Sub

    Private Sub Use(ByVal something As String, ByVal pid As Integer, ByVal somethingElse As Double)
    End Sub
End Class
");
        }
    }
}
