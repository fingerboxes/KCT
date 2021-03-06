﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalConstructionTime
{
    public class KCT_MathParsing
    {
        public static double GetStandardFormulaValue(string formulaName, Dictionary<string, string> variables)
        {
            switch (formulaName)
            {
                case "Node": return ParseMath(KCT_GameStates.formulaSettings.NodeFormula, variables);
                case "UpgradeFunds": return ParseMath(KCT_GameStates.formulaSettings.UpgradeFundsFormula, variables);
                case "UpgradeScience": return ParseMath(KCT_GameStates.formulaSettings.UpgradeScienceFormula, variables);
                case "Research": return ParseMath(KCT_GameStates.formulaSettings.ResearchFormula, variables);
                case "EffectivePart": return ParseMath(KCT_GameStates.formulaSettings.EffectivePartFormula, variables);
                case "ProceduralPart": return ParseMath(KCT_GameStates.formulaSettings.ProceduralPartFormula, variables);
                case "BP": return ParseMath(KCT_GameStates.formulaSettings.BPFormula, variables);
                case "KSCUpgrade": return ParseMath(KCT_GameStates.formulaSettings.KSCUpgradeFormula, variables);
                case "Reconditioning": return ParseMath(KCT_GameStates.formulaSettings.ReconditioningFormula, variables);
                case "BuildRate": return ParseMath(KCT_GameStates.formulaSettings.BuildRateFormula, variables);
                default: return 0;
            }
        }

        public static double ParseMath(string input, Dictionary<string, string> variables)
        {
           // KCTDebug.Log("Input_raw: " + input);
            string raw = input;
            foreach (KeyValuePair<string, string> kvp in variables)
            {
                if (input.Contains("[" + kvp.Key + "]"))
                {
                    input = input.Replace("[" + kvp.Key + "]", kvp.Value);
                }
            }
            //KCTDebug.Log("Input_replaced: " + input);

            double currentVal = 0;
            string stack = "";
            string lastOp = "+";
            string[] ops = { "+", "-", "*", "/", "%", "^", "(", "e", "E" };
            string[] functions = { "min", "max", "l", "L", "abs", "sign" };
            for (int i = 0; i < input.Length; ++i)
            {
                string ch = input[i].ToString();
                bool isOp = false, isFunction = false;
                //  KCTDebug.Log(ch);
                foreach (string op in ops)
                {
                    if (op == ch)
                    {
                        isOp = true;
                        break;
                    }
                }
                if (!isOp)
                {
                    foreach (string fun in functions)
                    {
                        if (fun[0] == input[i])
                        {
                            isFunction = true;
                            break;
                        }
                    }
                }

                if (isOp)
                {
                    if (ch == "-" && (stack.Trim() == ""))
                    {
                        stack += ch;
                    }
                    else if (ch == "e" || ch == "E")
                    {
                        int index;
                        for (index = i+2; index < input.Length; ++index)
                        {
                            string ch2 = input[index].ToString();
                            if (ops.Contains(ch2))
                                break;
                        }
                        string sub = input.Substring(i + 1, index - i - 1);
                        double exp = ParseMath(sub, variables);
                        double newVal = double.Parse(stack) * Math.Pow(10, exp);
                        currentVal = DoMath(currentVal, lastOp, newVal.ToString());
                        stack = "0";
                        lastOp = "+";
                        i = index - 1;
                    }
                    else if (ch == "(")
                    {
                        int j = FindEndParenthesis(input, i)[0];
                        string sub = input.Substring(i + 1, j - i - 1);
                        string val = ParseMath(sub, variables).ToString();
                        input = input.Substring(0, i) + val + input.Substring(j + 1);
                        --i;
                    }
                    else
                    {
                        currentVal = DoMath(currentVal, lastOp, stack);
                        lastOp = ch;
                        stack = "";
                    }
                }
                else if (isFunction)
                {
                    int subStart = input.IndexOf('(', i)+1;
                    string function = input.Substring(i, subStart - i - 1);
                    //KCTDebug.Log(function);
                    int[] parenComma = FindEndParenthesis(input, subStart-1);
                    int j = parenComma[0];
                    int comma = parenComma[1];
                    string sub = input.Substring(subStart, j - subStart);
                   // KCTDebug.Log("fn: "+function+" sub: "+sub);
                    double val = 0.0;

                    if (function == "l")
                    {
                        val = ParseMath(sub, variables);
                        val = Math.Log(val);
                    }
                    else if (function == "L")
                    {
                        val = ParseMath(sub, variables);
                        val = Math.Log10(val);
                    }
                    else if (function == "max" || function == "min")
                    {
                        string[] parts = new string[2];
                        parts[0] = input.Substring(subStart, parenComma[1] - subStart);
                        parts[1] = input.Substring(parenComma[1] + 1, j - parenComma[1] - 1);
                        double sub1 = ParseMath(parts[0], variables);
                        double sub2 = ParseMath(parts[1], variables);
                        if (function == "max")
                            val = Math.Max(sub1, sub2);
                        else if (function == "min")
                            val = Math.Min(sub1, sub2);
                    }
                    else if (function == "sign")
                    {
                        val = ParseMath(sub, variables);
                        if (val >= 0)
                            val = 1;
                        else
                            val = -1;
                    }
                    else if (function == "abs")
                    {
                        val = ParseMath(sub, variables);
                        val = Math.Abs(val);
                    }

                    input = input.Substring(0, i) + val.ToString() + input.Substring(j + 1);
                    i--;
                }
                else
                {
                    stack += ch;
                }
            }
            currentVal = DoMath(currentVal, lastOp, stack);
            KCTDebug.Log("(" + raw + ")=(" + input + ")=" + currentVal);
            return currentVal;
        }

        private static int[] FindEndParenthesis(string str, int curPos)
        {
            int depth = 0;
            int j = 0, commaPos = -1;
            for (j = curPos + 1; j < str.Length; ++j)
            {
                if (str[j] == '(') depth++;
                if (str[j] == ')') depth--;

                if (str[j] == ',' && depth == 0) 
                    commaPos = j;

                if (depth < 0)
                {
                    break;
                }
            }
            return new int[] {j, commaPos};
        }

        private static double DoMath(double currentVal, string operation, string newVal)
        {
            double newValue = 0;
            if (!double.TryParse(newVal, out newValue))
            {
                Debug.LogError("[KCT] Tried to parse a non-double value: " + newVal);
                return currentVal;
            }
            switch (operation)
            {
                case "+": currentVal += newValue; break;
                case "-": currentVal -= newValue; break;
                case "*": currentVal *= newValue; break;
                case "/": currentVal /= newValue; break;
                case "%": currentVal = currentVal % long.Parse(newVal); break;
                case "^": currentVal = Math.Pow(currentVal, newValue); break;
                default: break;
            }

            return currentVal;
        }

        public static double ParseBuildRateFormula(KCT_BuildListVessel.ListType type, int index, KCT_KSC KSC, bool UpgradedRates = false)
        {
            //N = num upgrades, I = rate index, L = VAB/SPH upgrade level, R = R&D level
            int level = 0, upgrades = 0;
            Dictionary<string, string> variables = new Dictionary<string, string>();
            if (type == KCT_BuildListVessel.ListType.VAB)
            {
                level = KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.VehicleAssemblyBuilding);
                if (KSC.VABUpgrades.Count > index)
                    upgrades = KSC.VABUpgrades[index];
            }
            else if (type == KCT_BuildListVessel.ListType.SPH)
            {
                level = KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.SpaceplaneHangar);
                if (KSC.SPHUpgrades.Count > index)
                    upgrades = KSC.SPHUpgrades[index];
            }
            if (UpgradedRates)
                upgrades++;
            variables.Add("L", level.ToString());
            variables.Add("N", upgrades.ToString());
            variables.Add("I", index.ToString());
            variables.Add("R", KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.ResearchAndDevelopment).ToString());

            return GetStandardFormulaValue("BuildRate", variables);
        }

        public static double ParseNodeRateFormula(double ScienceValue, bool UpgradedRates = false)
        {
            int RnDLvl = KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.ResearchAndDevelopment);
            int upgrades = KCT_GameStates.TechUpgradesTotal;
            if (UpgradedRates) upgrades++;
            Dictionary<string, string> variables = new Dictionary<string, string>();
            variables.Add("S", ScienceValue.ToString());
            variables.Add("N", upgrades.ToString());
            variables.Add("R", RnDLvl.ToString());

            return GetStandardFormulaValue("Node", variables);
        }
    }
}
