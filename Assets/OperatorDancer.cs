using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;
using Random = UnityEngine.Random;

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

    public static bool strictXnor(string element1, string element2)
    {
        if (element1.Length != element2.Length) return false;
        for (int i = 0; i < element1.Length; i++)
        {
            if ((element1[i] == '0' && element2[i] == '1') || (element1[i] == '1' && element2[i] == '0'))
            {
                Debug.Log("XNORing " + element1 + " and " + element2 + " equals FALSE. Index " + i);
                return false;
            }
        }
        Debug.Log("XNORing " + element1 + " and " + element2 + " equals TRUE.");
        return true;
    }

    public static string norTwoEquations(string element1, string element2)
    {
        if (element1.Length != element2.Length) return "";
        string ans = "";
        for (int i = 0; i < element1.Length; i++)
        {
            if (element1[i] == '0' && element2[i] == '0') ans += '1';
            else ans += '0';
        }
        return ans;
    }

    public static string invertEquation(string element)
    {
        return element.Replace('0', '2').Replace('1', '0').Replace('2', '1');
    }

    public static string norToTruthTable(string nor, int length)
    {
        Stack<string> stack = new Stack<string>();
        foreach (char c in nor)
        {
            if (length == 4) 
                switch (c)
                {
                    case '0': stack.Push("0000000011111111"); break;
                    case '1': stack.Push("0000111100001111"); break;
                    case '2': stack.Push("0011001100110011"); break;
                    case '3': stack.Push("0101010101010101"); break;
                    case '_':
                        if (stack.Count > 0) stack.Push(invertEquation(stack.Pop()));
                        else return ""; break;
                    case '|': if (stack.Count > 1) stack.Push(norTwoEquations(stack.Pop(),stack.Pop())); else return ""; break;
                    default: return "";
                }
            else
                switch (c)
                {
                    case '0': stack.Push("00000000000000001111111111111111"); break;
                    case '1': stack.Push("00000000111111110000000011111111"); break;
                    case '2': stack.Push("00001111000011110000111100001111"); break;
                    case '3': stack.Push("00110011001100110011001100110011"); break;
                    case '4': stack.Push("01010101010101010101010101010101"); break;
                    case '_':
                        if (stack.Count > 0) stack.Push(invertEquation(stack.Pop()));
                        else return ""; break;
                    case '|': if (stack.Count > 1) stack.Push(norTwoEquations(stack.Pop(),stack.Pop())); else return ""; break;
                    default: return "";
                }
        }
        return stack.Count != 1 ? "" : stack.Pop();
    }

    public static string convertEquToNor(string equ)
    {
        if (equ == "") return "FALSE";
        if (equ == "*****") return "TRUE";
        string[] sum = equ.Split('+');
        string result = String.Join("", Array.ConvertAll(sum, convertSingleToNor)).Replace("__","");
        for (int i = 0; i < sum.Length -1; i++) result += "|_";
        return result;
    }
}