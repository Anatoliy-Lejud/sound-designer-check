//#if UNITY_2018_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Assets.UBindr.Expressions;
using NUnit.Framework;
// ReSharper disable EqualExpressionComparison
// ReSharper disable ConditionIsAlwaysTrueOrFalse
  
namespace Assets.Tests
{
    [TestFixture]
    public class ExpressionTests
    {
        public static CSharpTopDownParser Parser { get; private set; }
        public static TopDownParser.Scope Scope { get; private set; }
        private static ViewModel vm;

        [SetUp]
        public void SetUp()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            Parser = new CSharpTopDownParser();
            Scope = new TopDownParser.Scope(Parser);

            vm = new ViewModel { Child = new ViewModel { Name = "Child Name", Index = 125 }, vm2 = new ViewModel { Val1 = 5 } };
            Scope.AddObjectRoot("vm", () => vm);
            Scope.AddObjectRoot("nullVm", () => null);
            Scope.AddObjectRoot("rootPi", () => vm.Pi);
            Scope.AddStaticRoot("Math", typeof(Math));
            Scope.AddStaticRoot("MyTime", typeof(MyTime));
        }

        [Test]
        public void ValidateAssignmentExpressions()
        {
            // Verify if expression is a proper assignment path
            ValidateAssignmentExpression("vm.one", true);
            ValidateAssignmentExpression("vm.Child", true);
            ValidateAssignmentExpression("vm.Child.one", true);
            ValidateAssignmentExpression("vm.one()", false);
            ValidateAssignmentExpression("5+2", false);
            ValidateAssignmentExpression("(true?vm:vm.Child).one", true);
            ValidateAssignmentExpression("(true?vm:vm.Child).one+1", false);
        }

        [Test]
        public void BreakDownPath()
        {
            ValidatePath("vm.Child.one", "vm|Child|one");
            ValidatePath("vm.GetOne().one", "vm|GetOne|one");
            ValidatePath("vm.GetOne().GetOne().one", "vm|GetOne|GetOne|one");
            ValidatePath("vm?.GetOne().GetOne().one", "vm|GetOne|GetOne|one");
            ValidatePath("vm?.(true?a:b).GetOne().one", "vm|(|GetOne|one");
        }

        [Test]
        public void RawEvaluationPerformanceTests()
        {
            Stopwatch sw = Stopwatch.StartNew();

            int iterations = 20000;
            TopDownParser.Scope scope = new TopDownParser.Scope(Parser);
            Parser.Evaluate("13+123+123+123+123", scope);
            for (int i = 0; i < iterations; i++)
            {
                Parser.Evaluate("13+123+123+123+123", scope);
            }
            WriteLine((float)sw.ElapsedMilliseconds / iterations + "ms/iteration");
        }

        [Test]
        public void ReusedEvaluationPerformanceTests()
        {
            Stopwatch sw = Stopwatch.StartNew();

            int iterations = 20000;
            TopDownParser.Scope scope = new TopDownParser.Scope(Parser);
            var expression = Parser.BuildExpression("13+123+123+123+123", scope);
            for (int i = 0; i < iterations; i++)
            {
                expression.Evaluate();
            }
            WriteLine((float)sw.ElapsedMilliseconds / iterations + "ms/iteration");
        }     

        [Test]
        public void ValidateParsing()
        {
            ValidateParse("yo.val.myFun=6", "(= (. (. yo val) myFun) 6)");

            ValidateParse("yo.val?.myFun()", "(?. (. yo val) myFun)");
            ValidateParse("yo.val.myFun()", "(. (. yo val) myFun)");
            ValidateParse("val.myFun()", "(. val myFun)");

            ValidateParse("myFun()", "myFun");
            ValidateParse("myFun(1+2)", "(myFun (+ 1 2))");
            ValidateParse("myFun(1,2,3,/*comment*/4)", "(myFun 1 2 3 4)");

            ValidateParse("myarr[1+2]", "(myarr (+ 1 2))");
            ValidateParse("myarr[1,2,3,/*comment*/4]", "(myarr 1 2 3 4)");

            ValidateParse("a=7", "(= a 7)");
            ValidateParse("!(a>b)", "(! (( (> a b)))");

            ValidateParse("a>b", "(> a b)");
            ValidateParse("a>=b+1", "(>= a (+ b 1))");

            ValidateParse("a<b", "(< a b)");
            ValidateParse("a<=b+1", "(<= a (+ b 1))");

            ValidateParse("a==b", "(== a b)");
            ValidateParse("a!=b+1", "(!= a (+ b 1))");

            ValidateParse("a&&b||c", "(|| (&& a b) c)");
            ValidateParse("a||b&&c", "(|| a (&& b c))");
            ValidateParse("2**3**4", "(** 2 (** 3 4))");

            ValidateParse("2**3**4", "(** 2 (** 3 4))");

            ValidateParse("true?(false?3:4):2+3", "(? true (( (? false 3 4)) (+ 2 3))");
            ValidateParse("true?(1):2+3", "(? true (( 1) (+ 2 3))");
            ValidateParse("true?1:2", "(? true 1 2)");

            ValidateParse("(1)", "(( 1)");
            ValidateParse("(1+2)", "(( (+ 1 2))");
            ValidateParse("((1*2)+(2*3))", "(( (+ (( (* 1 2)) (( (* 2 3))))");

            ValidateParse("2+3*4", "(+ 2 (* 3 4))");
            ValidateParse("2*3+4", "(+ (* 2 3) 4)");
            ValidateParse("2*3*4", "(* (* 2 3) 4)");
            ValidateParse("2+3+4", "(+ (+ 2 3) 4)");

            ValidateParse("-2", "(- 2)");
            ValidateParse("3+-2", "(+ 3 (- 2))");
        }

        [Test]
        public void SimpleEvaluate()
        {
            Validate("!true", !true);
            Validate("3+91", 3 + 91);
            Validate("3-91", 3 - 91);
            Validate("3", 3);
            Validate("'ooga'", "ooga");
        }

        [Test]
        public void TestEvaluatingOperators()
        {
            Validate(" 2 -1", 2 - 1);
            Validate(" 2 **3+3", (float)Math.Pow(2, 3) + 3);
            Validate(" 2**3**3", (float)Math.Pow(2, (float)Math.Pow(3, 3)));
            Validate(" 2 **3", (float)Math.Pow(2, 3));
            Validate(" 3 **-2", (float)Math.Pow(3, -2));
            Validate(" (1 +2 ) * ( 5    + 12 ) ", (1 + 2) * (5 + 12));
            Validate(" (2+2 **3+3 ) * ( 5    + 12 ) ", (2 + (float)Math.Pow(2, 3) + 3) * (5 + 12));

            Validate(" 5    + 12 ", 5 + 12);
            Validate(" 5.512    + 12 ", 5.512f + 12);
            Validate(" 5    + -12 ", 5 + -12);

            Validate(" 5    - 12 ", 5 - 12);
            Validate(" 5.512    - 12 ", 5.512f - 12);
            Validate(" 5    - -12 ", 5 - -12);

            Validate(" 5    * 12 ", 5 * 12);
            Validate(" 5.512    * 12 ", 5.512f * 12);
            Validate(" 5    * -12 ", 5 * -12);

            Validate(" 5f    / 12 ", 5f / 12);
            Validate(" 5.512    / 12 ", 5.512f / 12);
            Validate(" 5f    / -12 ", 5f / -12);
            Validate(" 10    / -2f ", 10 / -2f);

            Validate("(10-20-10-20)", (10 - 20 - 10 - 20));

            Validate("5f/10*2", 5f / 10 * 2);
            Validate("5f/10/2", 5f / 10 / 2);
            Validate("5f/(10*2)", 5f / (10 * 2));
            Validate("5f/(10/2)", 5f / (10f / 2));

            Validate("5f/10+2", 5f / 10 + 2);
            Validate("5f+10/2", 5f + 10f / 2);
            Validate("5+10/2", 5 + 10 / 2);
            Validate("5f/(10+2)", 5f / (10 + 2));
            Validate("5f+(10/2)", 5f + (10f / 2));

            Validate("5f/10-2", 5f / 10 - 2);
            Validate("5f-10/2", 5f - 10f / 2);
            Validate("5f/(10-2)", 5f / (10 - 2));
            Validate("5f-(10/2)", 5f - (10f / 2));
            Validate("-2.1", -2.1f);
            Validate("-2.1*-1.2", -2.1f * -1.2f);
            Validate("2.1", 2.1f);
            Validate("+2.1", +2.1f);

            Validate("(10-20)-(10-20)", (10 - 20) - (10 - 20));
            Validate("3 + 4 × 2 ÷ ( 1 − 5 ) ^ 2 ^ 3".Replace("^", "**"), 3 + 4 * 2 / (float)Math.Pow((1 - 5), (float)Math.Pow(2, 3)));
        }

        [Test]
        public void Numbers()
        {
            Validate(" 3*4+5 ", 3 * 4 + 5);

            Validate(" 3+4+5 ", 3 + 4 + 5);
            Validate(" 3*4*5 ", 3 * 4 * 5);
            Validate(" 3*4/5 ", 3 * 4 / 5);

            Validate(" 4/5 ", 4 / 5);
            Validate(" 1+2-3*4/5", 1 + 2 - 3 * 4 / 5);

            Validate(" 1 + 2 ", 1 + 2);
            Validate("1+2*3", 1 + 2 * 3);
            Validate("1+2*3f", 1 + 2 * 3f);
            Validate("1+2*3.0", 1 + 2 * 3.0f);
            Validate("1+2%3.0", 1 + 2 % 3.0f);
        }

        [Test]
        public void Booleans()
        {
            Validate("true==true", true == true);

            Validate("true", true);
            Validate("false", false);
            Validate("true && false", true && false);
            Validate("true && true", true && true);
            Validate("false && false", false && false);
            Validate("false && true", false && true);

            Validate("true && (false||true)", true && (false || true));

            Validate("!true", false);
            Validate("!false", true);

            Validate("-1>0", false);
            Validate("0>0", false);
            Validate("1>0", true);

            Validate("-1>=0", false);
            Validate("0>=0", true);
            Validate("1>=0", true);

            Validate("-1<0", true);
            Validate("0<0", false);
            Validate("1<0", false);

            Validate("-1<=0", true);
            Validate("0<=0", true);
            Validate("1<=0", false);

            Validate("1==1", true);
            Validate("1==0", false);

            Validate("1!=1", false);
            Validate("1!=0", true);

            Validate("10-1!=10", true);
            Validate("10-1<10", true);
            Validate("10-1>10", false);
            Validate("false==false", false == false);
            Validate("true!=true", true != true);
            Validate("false!=false", false != false);
            Validate("1!=0 == true", 1 != 0 == true);
        }

        [Test]
        public void TestAndOr()
        {
            Validate("true && true", true);
            Validate("true && false", false);
            Validate("false&& true ", false);
            Validate("false&& false ", false);

            Validate("true || true", true || true);
            Validate("true || false", true || false);
            Validate("false|| true ", false || true);
            Validate("false|| false ", false || false);

            Validate("false && true || false", false && true || false);
            Validate("false || true && false", false || true && false);

            Validate("true || false && true", true || false && true);
            Validate("true && false || true", true && false || true);

            Validate("true|| false && true", true || false && true);

            Validate("false|| true && true", false || true && true);

            Validate("true || false && true", true || false && true);

            Validate("false && (false || true || false)", false && (false || true || false));
            Validate("false && false || true || false", false && false || true || false);

            Validate("false && vm.throwException()", false);
            Validate("true || vm.throwException()", true);
        }

        [Test]
        public void TernaryTests()
        {
            Validate("true?1:2", true ? 1 : 2);
            Validate("false?1:2", false ? 1 : 2);
            Validate("2>1?1:2", 2 > 1 ? 1 : 2);
            Validate("-2>-1?-1:-2", -2 > -1 ? -1 : -2);
            Validate("2>1?(1<0?5:1):2", 2 > 1 ? (1 < 0 ? 5 : 1) : 2);
            Validate("2<1?(1<0?5:1):2", 2 < 1 ? (1 < 0 ? 5 : 1) : 2);
            Validate("true?1:vm.throwException()", 1);
            Validate("false?vm.throwException():2", 2);
        }

        [Test]
        public void ComparingValues()
        {
#pragma warning disable CS1718 // Comparison made to same variable
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
            Validate("null", null);
            Validate("'a'=='a'", "a" == "a");
            Validate("'a'=='b'", "a" == "b");
            Validate("'a'!='a'", "a" != "a");
            Validate("'a'!='b'", "a" != "b");
            Validate("null==null", null == null);
            Validate("1==null", 1 == null);
            Validate("1!=null", 1 != null);
            Validate("null==1", null == 1);
            Validate("null!=1", null != 1);
            Validate("1.1==null", 1.1 == null);
            Validate("1.1!=null", 1.1 != null);
            Validate("null==1.1", null == 1.1);
            Validate("null!=1.1", null != 1.1);

            Validate("vm.Child==null", vm.Child == null);
            Validate("vm.Child==vm.Child", vm.Child == vm.Child);

            Validate("vm.Child!=null", vm.Child != null);
            Validate("vm.Child!=vm.Child", vm.Child != vm.Child);
#pragma warning restore CS1718 // Comparison made to same variable
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
        }

        [Test]
        public void Comments()
        {
            Validate(" 3+4*5 /*With a comment*/", 3 + 4 * 5);

            Validate("'arne'", "arne");
            Validate("'arne'+'bertil'", "arnebertil");
        }

        [Test]
        public void Strings()
        {
            Validate("'arne'", "arne");
            Validate("'arne'+'bertil'", "arnebertil");
        }

        [Test]
        public void Parenthesis()
        {
            Validate(" (5) ", 5);
            Validate(" 1+(5) ", 1 + (5));
            Validate(" (1+5) ", (1 + 5));
            Validate(" (3*4+5) ", (3 * 4 + 5));
            Validate(" 1+(2-3)*4/5", 1 + (2 - 3) * 4 / 5);
            Validate(" 1+2-(3*4)/5", 1 + 2 - (3 * 4) / 5);
            Validate(" (3*4+5)*(3*4+5) ", (3 * 4 + 5) * (3 * 4 + 5));
        }

        [Test]
        public void TestEvaluatingVariables()
        {
            Validate("vm.Val1", vm.Val1);
            Validate("vm.vm2.Val1", vm.vm2.Val1);

            Validate("vm.time", ViewModel.time);
            Validate("10f / 2", 10f / 2);
            Validate("vm.Pi", vm.Pi);

            Validate("vm.Index+vm.Pi", vm.Index + vm.Pi);
            Validate("vm.Index/5f", vm.Index / 5f);
            Validate("vm.Index/vm.Pi", vm.Index / vm.Pi);
            Validate("vm.Index/-vm.Pi", vm.Index / (-vm.Pi));

            Validate(" (vm.Index +vm.Pi)", (vm.Index + vm.Pi));
            Validate(" (vm.Index *vm.Pi)", (vm.Index * vm.Pi));
            Validate(" (vm.Index -vm.Pi)", (vm.Index - vm.Pi));
            Validate(" (vm.Index / vm.Pi)", (vm.Index / vm.Pi));

            Validate(" 5    + 12 * vm.Index ", 5 + 12 * vm.Index);
            Validate(" 5    + 12 * (vm.Index +vm.Pi)", 5 + 12 * (vm.Index + vm.Pi));
            Validate(" 5    + 12 * (vm.Index /vm.Pi)", 5 + 12 * (vm.Index / vm.Pi));

            Validate("nullVm?.Pi", null);
            Validate("null", null);
            Validate("nullVm?.Pi==null", true);
            Validate("(true?vm:null).one", 1);
            Validate("MyTime.Time", MyTime.Time);
        }

        [Test]
        public void TestEvaluatingFunctions()
        {
            Validate("Math.Min (+10 , +2)", Math.Min(+10, +2));
            Validate("Math.Min (10 , -2)", Math.Min(10, -2));
            Validate("Math.Min (-10 , 2)", Math.Min(-10, 2));
            Validate("Math.Min (+10 , 2)", Math.Min(+10, 2));
            Validate("Math.Min (10 , 2)", Math.Min(10, 2));
            Validate("Math.Max(10 , 2)", Math.Max(10, 2));
            Validate("Math.Min(10 , 2)", Math.Min(10, 2));
            Validate("Math.Max(2, 10)", Math.Max(2, 10));
            Validate("Math.Min(2 , 10)", Math.Min(2, 10));

            Validate("Math.Max (5,10)", Math.Max(5, 10));
            //Validate("Math.Sin (2.1)", (float)Math.Sin(2.1f));
            //Validate("Math.Cos (2.1)", (float)Math.Cos(2.1f));

            Validate("vm.AddEx (+10 , +2)", vm.AddEx(+10, +2));

            Validate("Math.Max(vm.Index , rootPi)", (float)Math.Max(vm.Index, Math.PI));
        }

        [Test]
        public void IndexerTests()
        {
            Validate("vm.intArray[3-3]", vm.intArray[0]);
            Validate("vm.intArray[2]", vm.intArray[2]);
            Validate("vm[2*2]", vm[2 * 2]);
            Validate("vm.float2Array[1,1]", vm.float2Array[1, 1]);
            Validate("vm.strings[2]", vm.strings[2]);

            Validate("vm.intArray[2]", vm.intArray[2]);
            vm.vm2.intArray[2] = 99;
            Validate("vm.vm2.intArray[2]", vm.vm2.intArray[2]);
            Validate("vm['bob']", vm["bob"]);
            Validate("vm.vm2['bob']", vm.vm2["bob"]);
            Validate("vm.vm2[5]", vm.vm2[5]);
            Validate("vm[5]", vm[5]);

            vm.float2Array[1, 1] = -2.1f;
            Validate("vm.float2Array[1, 1]", vm.float2Array[1, 1]);
        }


        [Test]
        public void CanExecuteMethod()
        {
            Validate("vm.Testa(1,1)", vm.Testa(1, 1));
            Validate("vm.Testb(1,1)", vm.Testb(1, 1));
            Validate("vm.Testc(1,1)", vm.Testc(1, 1));
            // Doesn't work
            //Validate("vm.Enbatch(vm.Numbers,8)", vm.Enbatch(vm.Numbers, 8));
            // Works, but they don't return the same value
            //Validate("vm.Enbatched", vm.Enbatched);
        }

        [Test]
        public void IndexerGetTests2()
        {
            Validate("vm.Str.Length", vm.Str.Length);
            Validate("vm.strings[1].Length", vm.strings[1].Length);
            var expression = Parser.BuildExpression("vm.a=15", Scope);
            var actual = expression.Evaluate();
        }

        [Test]
        public void SetValueTests()
        {
            ValidateSetValue("vm.a", 12);
            ValidateSetValue("(true?vm:arne).a", 20);
            ValidateSetValue("(false?vm:vm.Child).a", 22);
            var expression = Parser.BuildExpression("vm.a", Scope);
            var actual = expression.Evaluate();
            WriteLine(actual.GetType().Name);
        }

        [Test]
        public void StringFormatTests()
        {
            Validate("'arne'", "arne");
            Validate("'arne {2+1}'", "arne 3");
            Validate("'arne [{2+1,4}]'", "arne [   3]");
            Validate("'arne [{2+1.1:0.00}]'", "arne [3.10]");
        }

        [Test]
        public void ValidateGetPathResultType()
        {
            ValidatePathType("vm.a", vm.a.GetType());
            ValidatePathType("vm.Child", vm.Child.GetType());
            ValidatePathType("vm.Child.a", vm.Child.a.GetType());
            ValidatePathType("vm.Name", vm.Name.GetType());
            ValidatePathType("vm.NullString", typeof(string));
        }

      
        [Test]
        public void GetAttribute()
        {
            var expression = Parser.BuildExpression("vm.Child.ranged", Scope);
            var attribute = CSharpTopDownParser.GetAttribute<MyAttribute>(expression);
            if (attribute == null)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DisableDuringExecuteWorks()
        {
            var expression = Parser.BuildExpression("vm.ThrowsError()", Scope);
            Scope.DisableExecute = true;
            expression.Evaluate();
            Scope.DisableExecute = false;
        }

        private void WriteLine(object obj)
        {
#if UNITY_X_Y_OR_NEWER
#else
            if (obj == null)
            {
                //Debug.Log("NULL");
            }
            else
            {
                Console.Error.WriteLine(obj.ToString());
            }
#endif
        }

        public void ValidateParse(string code, string parsed)
        {
            var actual = Parser.BuildExpression(code, Scope).ToString();
            WriteLine(string.Format("{0} => {1} [{2}]", code, parsed, parsed));
            if (actual != parsed)
            {
                Assert.Fail();
            }
        }

        public void ValidatePathType(string code, Type expectedType)
        {
            Type actual = Parser.GetPathResultType(code, Scope);
            WriteLine(string.Format("{0} => {1} [{2}]", code, actual, expectedType));
            if (actual != expectedType)
            {
                Assert.Fail();
            }
        }

        public void Validate(string code, object expected)
        {
            code = code.Replace("'", "\"");
            var expression = Parser.BuildExpression(code, Scope);
            var actual = expression.Evaluate();
            WriteLine(string.Format("[{0}] = {1} ({2}, {3})", code, actual, expected, expression));
            Assert.That(Equals(actual, expected), string.Format("Expected {0} but received {1}", expected, actual));
        }

        public void ValidateSetValue(string code, object value)
        {
            code = code.Replace("'", "\"");
            var expression = Parser.BuildExpression(code, Scope);
            Parser.SetValue(expression, value);
            var actual = expression.Evaluate();
            WriteLine(string.Format("[{0}] = {1} => {2} ({3})", code, value, actual, expression));
            Assert.That(Equals(actual, value), string.Format("Expected {0} but received {1}", value, actual));
        }

        private void ValidatePath(string code, string expectedPath)
        {
            var expression = Parser.BuildExpression(code, Scope);
            string path = expression.GetTokenPath().SJoin(x => x.Text, "|");
            WriteLine(string.Format("{0} => {1} ({2})", code, path, expression));
            Assert.AreEqual(path, expectedPath);
        }

        private void ValidateAssignmentExpression(string code, bool shouldBeValid)
        {
            var expression = Parser.BuildExpression(code, Scope);
            bool isValid = Parser.GetIsValidSetValueExpression(expression);
            WriteLine(string.Format("{0} => {1} ({2})", code, isValid, shouldBeValid));
            Assert.AreEqual(isValid, shouldBeValid);
        }

        public class MyAttribute : Attribute
        {
            public MyAttribute(float minValue, float maxValue)
            {
                MinValue = minValue;
                MaxValue = maxValue;
            }

            public float MinValue { get; private set; }
            public float MaxValue { get; private set; }

            public override string ToString()
            {
                return string.Format("My {0}-{1}", MinValue, MaxValue);
            }
        }

        public static class MyTime
        {
            public static float Time { get { return 11.22f; } }
        }

        public class ViewModel
        {
            public ViewModel()
            {
                Numbers = Enumerable.Range(0, 64).Select(x => new Number { Value = x }).ToList();
                Str = "Ooga Booga";
            }

            public void ThrowsError()
            {
                throw new InvalidOperationException("Error!");
            }
            public int one = 1;
            public int zero = 1;
            public int a = 1;
            public int Index = 10;
            public float Pi = 3.14159f;
            public string Name = "myName";
            public float VariableF = 10;
            public bool VariableB = true;
            public string Str { get; set; }

            [My(-20, 100)]
            public float ranged = 0.2f;
            public string NullString = null;
            public ViewModel Child;
            public static float time = 3.12f;

            public string[] strings = new[] { "a", "b", "c", "d" };

            public int[] intArray = new int[] { 0, -1, -2, -3, -4 };
            public float[,] float2Array = new float[,] { { 0, -1 }, { 1, 2 } };

            public float Val1 = 2;
            public ViewModel vm2;

            public int this[string index] { get { return index.GetHashCode(); } }
            public int this[int index] { get { return index + 2; } }

            public int Mindex
            {
                private get { return Index; }
                set { Index = value; }
            }

            public int GetIndex() { return Index; }
            public float AddEx(int x, int y) { return x + y; }
            public float AddEx(float x, int y) { return x + y - 1; }
            public static float SAddEx(int x, int y) { return x + y; }
            public static float SAddEx(float x, int y) { return x + y - 1; }
            public List<Number> Numbers { get; set; }

            public class Number
            {
                public int Value { get; set; }
            }

            public IEnumerable<IEnumerable<Number>> Enbatched
            {
                get
                {
                    return Enbatch(Numbers, 8);
                }
            }

            public float Testa(float a, float b)
            {
                return a + b;
            }

            public float Testb(int a, float b)
            {
                return a + b;
            }

            public int Testc(int a, int b)
            {
                return a + b;
            }

            public IEnumerable<IEnumerable<T>> Enbatch<T>(IEnumerable<T> items, int batchSize)
            {
                var left = items.ToList();
                while (left.Any())
                {
                    yield return left.Take(batchSize);
                    left = left.Skip(batchSize).ToList();
                }
            }

            public void NoResult()
            {
                Console.Error.WriteLine("There will be no result");
            }
        }
    }
}
//#endif