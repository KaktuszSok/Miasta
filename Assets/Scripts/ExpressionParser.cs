/* * * * * * * * * * * * * *
 * A simple expression parser
 * --------------------------
 * 
 * The parser can parse a mathematical expression into a simple custom
 * expression tree. It can recognise methods and fields/contants which
 * are user extensible. It can also contain expression parameters which
 * are registrated automatically. An expression tree can be "converted"
 * into a delegate.
 * 
 * Written by Bunny83
 * 2014-11-02
 * 
 * Features:
 * - Elementary arithmetic [ + - * / ]
 * - Power [ ^ ]
 * - Brackets ( )
 * - Most function from System.Math (abs, sin, round, floor, min, ...)
 * - Constants ( e, PI )
 * - MultiValue return (quite slow, produce extra garbage each call)
 * 
 * * * * * * * * * * * * * */
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace B83.ExpressionParser
{
    public interface IValue
    {
        float Value { get; }
    }
    public class Number : IValue
    {
        private float m_Value;
        public float Value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }
        public Number(float aValue)
        {
            m_Value = aValue;
        }
        public override string ToString()
        {
            return "" + m_Value + "";
        }
    }
    public class OperationSum : IValue
    {
        private IValue[] m_Values;
        public float Value
        {
            get { return m_Values.Select(v => v.Value).Sum(); }
        }
        public OperationSum(params IValue[] aValues)
        {
            // collapse unnecessary nested sum operations.
            List<IValue> v = new List<IValue>(aValues.Length);
            foreach (var I in aValues)
            {
                var sum = I as OperationSum;
                if (sum == null)
                    v.Add(I);
                else
                    v.AddRange(sum.m_Values);
            }
            m_Values = v.ToArray();
        }
        public override string ToString()
        {
            return "( " + string.Join(" + ", m_Values.Select(v => v.ToString()).ToArray()) + " )";
        }
    }
    public class OperationProduct : IValue
    {
        private IValue[] m_Values;
        public float Value
        {
            get { return m_Values.Select(v => v.Value).Aggregate((v1, v2) => v1 * v2); }
        }
        public OperationProduct(params IValue[] aValues)
        {
            m_Values = aValues;
        }
        public override string ToString()
        {
            return "( " + string.Join(" * ", m_Values.Select(v => v.ToString()).ToArray()) + " )";
        }

    }
    public class OperationPower : IValue
    {
        private IValue m_Value;
        private IValue m_Power;
        public float Value
        {
            get { return Mathf.Pow(m_Value.Value, m_Power.Value); }
        }
        public OperationPower(IValue aValue, IValue aPower)
        {
            m_Value = aValue;
            m_Power = aPower;
        }
        public override string ToString()
        {
            return "( " + m_Value + "^" + m_Power + " )";
        }

    }
    public class OperationNegate : IValue
    {
        private IValue m_Value;
        public float Value
        {
            get { return -m_Value.Value; }
        }
        public OperationNegate(IValue aValue)
        {
            m_Value = aValue;
        }
        public override string ToString()
        {
            return "( -" + m_Value + " )";
        }

    }
    public class OperationReciprocal : IValue
    {
        private IValue m_Value;
        public float Value
        {
            get { return 1.0f / m_Value.Value; }
        }
        public OperationReciprocal(IValue aValue)
        {
            m_Value = aValue;
        }
        public override string ToString()
        {
            return "( 1/" + m_Value + " )";
        }

    }

    public class MultiParameterList : IValue
    {
        private IValue[] m_Values;
        public IValue[] Parameters { get { return m_Values; } }
        public float Value
        {
            get { return m_Values.Select(v => v.Value).FirstOrDefault(); }
        }
        public MultiParameterList(params IValue[] aValues)
        {
            m_Values = aValues;
        }
        public override string ToString()
        {
            return string.Join(", ", m_Values.Select(v => v.ToString()).ToArray());
        }
    }

    public class CustomFunction : IValue
    {
        private IValue[] m_Params;
        private System.Func<float[], float> m_Delegate;
        private string m_Name;
        public float Value
        {
            get
            {
                if (m_Params == null)
                    return m_Delegate(null);
                return m_Delegate(m_Params.Select(p => p.Value).ToArray());
            }
        }
        public CustomFunction(string aName, System.Func<float[], float> aDelegate, params IValue[] aValues)
        {
            m_Delegate = aDelegate;
            m_Params = aValues;
            m_Name = aName;
        }
        public override string ToString()
        {
            if (m_Params == null)
                return m_Name;
            return m_Name + "( " + string.Join(", ", m_Params.Select(v => v.ToString()).ToArray()) + " )";
        }
    }
    public class Parameter : Number
    {
        public string Name { get; private set; }
        public override string ToString()
        {
            return Name + "[" + base.ToString() + "]";
        }
        public Parameter(string aName) : base(0)
        {
            Name = aName;
        }
    }

    public class Expression : IValue
    {
        public Dictionary<string, Parameter> Parameters = new Dictionary<string, Parameter>();
        public IValue ExpressionTree { get; set; }
        public float Value
        {
            get { return ExpressionTree.Value; }
        }
        public float[] MultiValue
        {
            get
            {
                var t = ExpressionTree as MultiParameterList;
                if (t != null)
                {
                    float[] res = new float[t.Parameters.Length];
                    for (int i = 0; i < res.Length; i++)
                        res[i] = t.Parameters[i].Value;
                    return res;
                }
                return null;
            }
        }
        public override string ToString()
        {
            return ExpressionTree.ToString();
        }
        public ExpressionDelegate ToDelegate(params string[] aParamOrder)
        {
            var parameters = new List<Parameter>(aParamOrder.Length);
            for (int i = 0; i < aParamOrder.Length; i++)
            {
                if (Parameters.ContainsKey(aParamOrder[i]))
                    parameters.Add(Parameters[aParamOrder[i]]);
                else
                    parameters.Add(null);
            }
            var parameters2 = parameters.ToArray();

            return (p) => Invoke(p, parameters2);
        }
        public MultiResultDelegate ToMultiResultDelegate(params string[] aParamOrder)
        {
            var parameters = new List<Parameter>(aParamOrder.Length);
            for (int i = 0; i < aParamOrder.Length; i++)
            {
                if (Parameters.ContainsKey(aParamOrder[i]))
                    parameters.Add(Parameters[aParamOrder[i]]);
                else
                    parameters.Add(null);
            }
            var parameters2 = parameters.ToArray();


            return (p) => InvokeMultiResult(p, parameters2);
        }
        float Invoke(float[] aParams, Parameter[] aParamList)
        {
            int count = System.Math.Min(aParamList.Length, aParams.Length);
            for (int i = 0; i < count; i++)
            {
                if (aParamList[i] != null)
                    aParamList[i].Value = aParams[i];
            }
            return Value;
        }
        float[] InvokeMultiResult(float[] aParams, Parameter[] aParamList)
        {
            int count = System.Math.Min(aParamList.Length, aParams.Length);
            for (int i = 0; i < count; i++)
            {
                if (aParamList[i] != null)
                    aParamList[i].Value = aParams[i];
            }
            return MultiValue;
        }
        public static Expression Parse(string aExpression)
        {
            return new ExpressionParser().EvaluateExpression(aExpression);
        }

        public class ParameterException : System.Exception { public ParameterException(string aMessage) : base(aMessage) { } }
    }
    public delegate float ExpressionDelegate(params float[] aParams);
    public delegate float[] MultiResultDelegate(params float[] aParams);



    public class ExpressionParser
    {
        private List<string> m_BracketHeap = new List<string>();
        private Dictionary<string, System.Func<float>> m_Consts = new Dictionary<string, System.Func<float>>();
        private Dictionary<string, System.Func<float[], float>> m_Funcs = new Dictionary<string, System.Func<float[], float>>();
        private Expression m_Context;

        public ExpressionParser()
        {
            m_Consts.Add("PI", () => Mathf.PI);
            m_Consts.Add("e", () => (float)System.Math.E);
            m_Funcs.Add("sqrt", (p) => Mathf.Sqrt(p.FirstOrDefault()));
            m_Funcs.Add("abs", (p) => Mathf.Abs(p.FirstOrDefault()));
            m_Funcs.Add("ln", (p) => Mathf.Log(p.FirstOrDefault()));
            m_Funcs.Add("floor", (p) => Mathf.Floor(p.FirstOrDefault()));
            m_Funcs.Add("ceiling", (p) => Mathf.Ceil(p.FirstOrDefault()));
            m_Funcs.Add("round", (p) => Mathf.Round(p.FirstOrDefault()));

            m_Funcs.Add("sin", (p) => Mathf.Sin(p.FirstOrDefault()));
            m_Funcs.Add("cos", (p) => Mathf.Cos(p.FirstOrDefault()));
            m_Funcs.Add("tan", (p) => Mathf.Tan(p.FirstOrDefault()));

            m_Funcs.Add("asin", (p) => Mathf.Asin(p.FirstOrDefault()));
            m_Funcs.Add("acos", (p) => Mathf.Acos(p.FirstOrDefault()));
            m_Funcs.Add("atan", (p) => Mathf.Atan(p.FirstOrDefault()));
            m_Funcs.Add("atan2", (p) => Mathf.Atan2(p.FirstOrDefault(), p.ElementAtOrDefault(1)));
            //System.Math.Floor
            m_Funcs.Add("min", (p) => Mathf.Min(p.FirstOrDefault(), p.ElementAtOrDefault(1)));
            m_Funcs.Add("max", (p) => Mathf.Max(p.FirstOrDefault(), p.ElementAtOrDefault(1)));
            m_Funcs.Add("rnd", (p) =>
            {
                if (p.Length == 2)
                    return p[0] + Random.value * (p[1] - p[0]);
                if (p.Length == 1)
                    return Random.value * p[0];
                return Random.value;
            });
        }

        public void AddFunc(string aName, System.Func<float[], float> aMethod)
        {
            if (m_Funcs.ContainsKey(aName))
                m_Funcs[aName] = aMethod;
            else
                m_Funcs.Add(aName, aMethod);
        }

        public void AddConst(string aName, System.Func<float> aMethod)
        {
            if (m_Consts.ContainsKey(aName))
                m_Consts[aName] = aMethod;
            else
                m_Consts.Add(aName, aMethod);
        }
        public void RemoveFunc(string aName)
        {
            if (m_Funcs.ContainsKey(aName))
                m_Funcs.Remove(aName);
        }
        public void RemoveConst(string aName)
        {
            if (m_Consts.ContainsKey(aName))
                m_Consts.Remove(aName);
        }

        int FindClosingBracket(ref string aText, int aStart, char aOpen, char aClose)
        {
            int counter = 0;
            for (int i = aStart; i < aText.Length; i++)
            {
                if (aText[i] == aOpen)
                    counter++;
                if (aText[i] == aClose)
                    counter--;
                if (counter == 0)
                    return i;
            }
            return -1;
        }

        void SubstitudeBracket(ref string aExpression, int aIndex)
        {
            int closing = FindClosingBracket(ref aExpression, aIndex, '(', ')');
            if (closing > aIndex + 1)
            {
                string inner = aExpression.Substring(aIndex + 1, closing - aIndex - 1);
                m_BracketHeap.Add(inner);
                string sub = "&" + (m_BracketHeap.Count - 1) + ";";
                aExpression = aExpression.Substring(0, aIndex) + sub + aExpression.Substring(closing + 1);
            }
            else throw new ParseException("Bracket not closed!");
        }

        IValue Parse(string aExpression)
        {
            aExpression = aExpression.Trim();
            int index = aExpression.IndexOf('(');
            while (index >= 0)
            {
                SubstitudeBracket(ref aExpression, index);
                index = aExpression.IndexOf('(');
            }
            if (aExpression.Contains(','))
            {
                string[] parts = aExpression.Split(',');
                List<IValue> exp = new List<IValue>(parts.Length);
                for (int i = 0; i < parts.Length; i++)
                {
                    string s = parts[i].Trim();
                    if (!string.IsNullOrEmpty(s))
                        exp.Add(Parse(s));
                }
                return new MultiParameterList(exp.ToArray());
            }
            else if (aExpression.Contains('+'))
            {
                string[] parts = aExpression.Split('+');
                List<IValue> exp = new List<IValue>(parts.Length);
                for (int i = 0; i < parts.Length; i++)
                {
                    string s = parts[i].Trim();
                    if (!string.IsNullOrEmpty(s))
                        exp.Add(Parse(s));
                }
                if (exp.Count == 1)
                    return exp[0];
                return new OperationSum(exp.ToArray());
            }
            else if (aExpression.Contains('-'))
            {
                string[] parts = aExpression.Split('-');
                List<IValue> exp = new List<IValue>(parts.Length);
                if (!string.IsNullOrEmpty(parts[0].Trim()))
                    exp.Add(Parse(parts[0]));
                for (int i = 1; i < parts.Length; i++)
                {
                    string s = parts[i].Trim();
                    if (!string.IsNullOrEmpty(s))
                        exp.Add(new OperationNegate(Parse(s)));
                }
                if (exp.Count == 1)
                    return exp[0];
                return new OperationSum(exp.ToArray());
            }
            else if (aExpression.Contains('*'))
            {
                string[] parts = aExpression.Split('*');
                List<IValue> exp = new List<IValue>(parts.Length);
                for (int i = 0; i < parts.Length; i++)
                {
                    exp.Add(Parse(parts[i]));
                }
                if (exp.Count == 1)
                    return exp[0];
                return new OperationProduct(exp.ToArray());
            }
            else if (aExpression.Contains('/'))
            {
                string[] parts = aExpression.Split('/');
                List<IValue> exp = new List<IValue>(parts.Length);
                if (!string.IsNullOrEmpty(parts[0].Trim()))
                    exp.Add(Parse(parts[0]));
                for (int i = 1; i < parts.Length; i++)
                {
                    string s = parts[i].Trim();
                    if (!string.IsNullOrEmpty(s))
                        exp.Add(new OperationReciprocal(Parse(s)));
                }
                return new OperationProduct(exp.ToArray());
            }
            else if (aExpression.Contains('^'))
            {
                int pos = aExpression.IndexOf('^');
                var val = Parse(aExpression.Substring(0, pos));
                var pow = Parse(aExpression.Substring(pos + 1));
                return new OperationPower(val, pow);
            }
            int pPos = aExpression.IndexOf("&");
            if (pPos > 0)
            {
                string fName = aExpression.Substring(0, pPos);
                foreach (var M in m_Funcs)
                {
                    if (fName == M.Key)
                    {
                        var inner = aExpression.Substring(M.Key.Length);
                        var param = Parse(inner);
                        var multiParams = param as MultiParameterList;
                        IValue[] parameters;
                        if (multiParams != null)
                            parameters = multiParams.Parameters;
                        else
                            parameters = new IValue[] { param };
                        return new CustomFunction(M.Key, M.Value, parameters);
                    }
                }
            }
            foreach (var C in m_Consts)
            {
                if (aExpression == C.Key)
                {
                    return new CustomFunction(C.Key, (p) => C.Value(), null);
                }
            }
            int index2a = aExpression.IndexOf('&');
            int index2b = aExpression.IndexOf(';');
            if (index2a >= 0 && index2b >= 2)
            {
                var inner = aExpression.Substring(index2a + 1, index2b - index2a - 1);
                int bracketIndex;
                if (int.TryParse(inner, out bracketIndex) && bracketIndex >= 0 && bracketIndex < m_BracketHeap.Count)
                {
                    return Parse(m_BracketHeap[bracketIndex]);
                }
                else
                    throw new ParseException("Can't parse substitude token");
            }
            float floatValue;
            if (float.TryParse(aExpression, out floatValue))
            {
                return new Number(floatValue);
            }
            if (ValidIdentifier(aExpression))
            {
                if (m_Context.Parameters.ContainsKey(aExpression))
                    return m_Context.Parameters[aExpression];
                var val = new Parameter(aExpression);
                m_Context.Parameters.Add(aExpression, val);
                return val;
            }

            throw new ParseException("Reached unexpected end within the parsing tree");
        }

        private bool ValidIdentifier(string aExpression)
        {
            aExpression = aExpression.Trim();
            if (string.IsNullOrEmpty(aExpression))
                return false;
            if (aExpression.Length < 1)
                return false;
            if (aExpression.Contains(" "))
                return false;
            if (!"abcdefghijklmnopqrstuvwxyz§$".Contains(char.ToLower(aExpression[0])))
                return false;
            if (m_Consts.ContainsKey(aExpression))
                return false;
            if (m_Funcs.ContainsKey(aExpression))
                return false;
            return true;
        }

        public Expression EvaluateExpression(string aExpression)
        {
            var val = new Expression();
            m_Context = val;
            val.ExpressionTree = Parse(aExpression);
            m_Context = null;
            m_BracketHeap.Clear();
            return val;
        }

        public float Evaluate(string aExpression)
        {
            return EvaluateExpression(aExpression).Value;
        }
        public static float Eval(string aExpression)
        {
            return new ExpressionParser().Evaluate(aExpression);
        }

        public class ParseException : System.Exception { public ParseException(string aMessage) : base(aMessage) { } }
    }
}