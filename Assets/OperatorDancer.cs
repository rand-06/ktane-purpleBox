using System.Collections.Generic;
using System.Linq;

public static class OperatorDancer //танцор, потому что это еще те танцы с бубнами :pensive:
{

    private static string convertSingleToNor(string element)
    {
        string ans = "";
        for(int i = 0; i < element.Length; i++)
        {
            if (element[i] == '*') continue;
            if (ans == "")
            {
                ans += i.ToString();
                if (element[i] == '0') ans += '_';
            }
            else
            {
                ans += "_";
                ans += i.ToString();
                if (element[i] == '1') ans += '_';
                ans += '|';
            }
        }
        return ans;
    }

    public static bool strictXnor(string element1, string element2, int ModuleId)
    {
        if (element1.Length != element2.Length) return false;
        bool ans = Enumerable.Range(0, element1.Length).All(i => element1[i] == '*' || element2[i] == '*' || element1[i] == element2[i]);
        UnityEngine.Debug.Log("[Insomnious Automaton #"+ ModuleId +"] XNORing " + element1 + " and " + element2 + " equals "+ ans.ToString().ToUpper() +".");
        return ans;
    }

    private static string norTwoEquations(string element1, string element2) => (element1.Length == element2.Length)?
        Enumerable.Range(0, element1.Length).Select(i => (element1[i] == '0' && element2[i] == '0')?'1':'0').Aggregate("",(a,b)=>a+b):"";

    private static string invertEquation(string element)=>element.Replace('0', '2').Replace('1', '0').Replace('2', '1');

    private static string stackString(int length, int query) => Enumerable.Range(0, 1<<length).Select(x => 
        (x % (1 << (length - query)))/(1 << (length - query - 1))).Aggregate("",(a,b)=>a+b);
    
    
    public static string norToTruthTable(string nor, int length)
    {
        Stack<string> stack = new Stack<string>();
        foreach (char c in nor) switch (c)
        {
            case '_': if (stack.Count > 0) stack.Push(invertEquation(stack.Pop())); else return ""; break;
            case '|': if (stack.Count > 1) stack.Push(norTwoEquations(stack.Pop(),stack.Pop())); else return ""; break;
            default:  if ((c - '0') < length) stack.Push(stackString(length, c - '0')); else return ""; break;
        }
        return stack.Count != 1 ? "" : stack.Pop();
    }

    public static string convertEquToNor(string equ)
    {
        if (equ == "") return "FALSE";
        if (equ == "*****") return "TRUE";
        string[] sum = equ.Split('+');
        string result = sum.Select(convertSingleToNor).Aggregate("", (a, b) => a + b).Replace("__","");
        for (int i = 0; i < sum.Length -1; i++) result += "|_";
        return result;
    }
}