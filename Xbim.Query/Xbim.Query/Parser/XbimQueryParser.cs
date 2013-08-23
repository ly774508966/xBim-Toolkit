// This code was generated by the Gardens Point Parser Generator
// Copyright (c) Wayne Kelly, QUT 2005-2010
// (see accompanying GPPGcopyright.rtf)

// GPPG version 1.5.0
// Machine:  CENTAURUS
// DateTime: 23.8.2013 15:43:53
// UserName: Martin
// Input file <XbimQueryParser.y - 23.8.2013 15:43:49>

// options: conflicts lines gplex conflicts

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QUT.Gppg;

namespace Xbim.Query
{
public enum Tokens {error=60,
    EOF=61,INTEGER=62,FLOAT=63,STRING=64,BOOLEAN=65,NONDEF=66,
    IDENTIFIER=67,OP_EQ=68,OP_NEQ=69,OP_GT=70,OP_LT=71,OP_GTE=72,
    OP_LTQ=73,OP_CONTAINS=74,OP_NOT_CONTAINS=75,OP_AND=76,OP_OR=77,PRODUCT=78,
    PRODUCT_TYPE=79,SELECT=80,WHERE=81,CREATE=82,WITH_NAME=83,DESCRIPTION=84,
    NEW=85,ADD=86,TO=87,REMOVE=88,FROM=89,NAME=90,
    PREDEFINED_TYPE=91,TYPE=92,MATERIAL=93};

public partial struct ValueType
#line 11 "XbimQueryParser.y"
{
#line 12 "XbimQueryParser.y"
		public string strVal;
#line 13 "XbimQueryParser.y"
	  }
// Abstract base class for GPLEX scanners
public abstract class ScanBase : AbstractScanner<ValueType,LexLocation> {
  private LexLocation __yylloc = new LexLocation();
  public override LexLocation yylloc { get { return __yylloc; } set { __yylloc = value; } }
  protected virtual bool yywrap() { return true; }
}

// Utility class for encapsulating token information
public class ScanObj {
  public int token;
  public ValueType yylval;
  public LexLocation yylloc;
  public ScanObj( int t, ValueType val, LexLocation loc ) {
    this.token = t; this.yylval = val; this.yylloc = loc;
  }
}

public partial class XbimQueryParser: ShiftReduceParser<ValueType, LexLocation>
{
  // Verbatim content from XbimQueryParser.y - 23.8.2013 15:43:49
#line 2 "XbimQueryParser.y"
 
  // End verbatim content from XbimQueryParser.y - 23.8.2013 15:43:49

#pragma warning disable 649
  private static Dictionary<int, string> aliasses;
#pragma warning restore 649
  private static Rule[] rules = new Rule[48];
  private static State[] states = new State[86];
  private static string[] nonTerms = new string[] {
      "expression", "$accept", "selection", "creation", "addition", "conditions", 
      "condition", "attributeCondidion", "materialCondition", "typeCondition", 
      "propertyCondition", "attribute", "op_bool", "op_cont", "op_num_rel", };

  static XbimQueryParser() {
    states[0] = new State(-5,new int[]{-1,1});
    states[1] = new State(new int[]{61,2,80,9,67,60,82,72,86,78,88,82},new int[]{-3,3,-4,5,-5,7});
    states[2] = new State(-1);
    states[3] = new State(new int[]{59,4});
    states[4] = new State(-2);
    states[5] = new State(new int[]{59,6});
    states[6] = new State(-3);
    states[7] = new State(new int[]{59,8});
    states[8] = new State(-4);
    states[9] = new State(new int[]{78,10});
    states[10] = new State(new int[]{81,11,59,-6});
    states[11] = new State(new int[]{90,26,84,27,91,28,93,30,92,36,64,42,76,-18,77,-18,59,-18},new int[]{-6,12,-7,13,-8,16,-12,17,-9,29,-10,35,-11,41});
    states[12] = new State(-7);
    states[13] = new State(new int[]{76,14,77,58,59,-17});
    states[14] = new State(new int[]{90,26,84,27,91,28,93,30,92,36,64,42,59,-18},new int[]{-7,15,-8,16,-12,17,-9,29,-10,35,-11,41});
    states[15] = new State(-15);
    states[16] = new State(-19);
    states[17] = new State(new int[]{68,22,69,23,74,24,75,25},new int[]{-13,18,-14,20});
    states[18] = new State(new int[]{64,19});
    states[19] = new State(-23);
    states[20] = new State(new int[]{64,21});
    states[21] = new State(-24);
    states[22] = new State(-40);
    states[23] = new State(-41);
    states[24] = new State(-46);
    states[25] = new State(-47);
    states[26] = new State(-25);
    states[27] = new State(-26);
    states[28] = new State(-27);
    states[29] = new State(-20);
    states[30] = new State(new int[]{68,22,69,23,74,24,75,25},new int[]{-13,31,-14,33});
    states[31] = new State(new int[]{64,32});
    states[32] = new State(-28);
    states[33] = new State(new int[]{64,34});
    states[34] = new State(-29);
    states[35] = new State(-21);
    states[36] = new State(new int[]{68,37,69,39});
    states[37] = new State(new int[]{79,38});
    states[38] = new State(-30);
    states[39] = new State(new int[]{79,40});
    states[40] = new State(-31);
    states[41] = new State(-22);
    states[42] = new State(new int[]{68,22,69,23,70,54,71,55,72,56,73,57,74,24,75,25},new int[]{-13,43,-15,49,-14,52});
    states[43] = new State(new int[]{62,44,63,45,64,46,65,47,66,48});
    states[44] = new State(-32);
    states[45] = new State(-34);
    states[46] = new State(-36);
    states[47] = new State(-38);
    states[48] = new State(-39);
    states[49] = new State(new int[]{62,50,63,51});
    states[50] = new State(-33);
    states[51] = new State(-35);
    states[52] = new State(new int[]{64,53});
    states[53] = new State(-37);
    states[54] = new State(-42);
    states[55] = new State(-43);
    states[56] = new State(-44);
    states[57] = new State(-45);
    states[58] = new State(new int[]{90,26,84,27,91,28,93,30,92,36,64,42,59,-18},new int[]{-7,59,-8,16,-12,17,-9,29,-10,35,-11,41});
    states[59] = new State(-16);
    states[60] = new State(new int[]{68,61,69,65,85,69});
    states[61] = new State(new int[]{78,62});
    states[62] = new State(new int[]{81,63});
    states[63] = new State(new int[]{90,26,84,27,91,28,93,30,92,36,64,42,76,-18,77,-18,59,-18},new int[]{-6,64,-7,13,-8,16,-12,17,-9,29,-10,35,-11,41});
    states[64] = new State(-8);
    states[65] = new State(new int[]{78,66});
    states[66] = new State(new int[]{81,67});
    states[67] = new State(new int[]{90,26,84,27,91,28,93,30,92,36,64,42,76,-18,77,-18,59,-18},new int[]{-6,68,-7,13,-8,16,-12,17,-9,29,-10,35,-11,41});
    states[68] = new State(-9);
    states[69] = new State(new int[]{78,70});
    states[70] = new State(new int[]{64,71});
    states[71] = new State(-12);
    states[72] = new State(new int[]{78,73});
    states[73] = new State(new int[]{83,74});
    states[74] = new State(new int[]{64,75});
    states[75] = new State(new int[]{84,76,59,-10});
    states[76] = new State(new int[]{64,77});
    states[77] = new State(-11);
    states[78] = new State(new int[]{67,79});
    states[79] = new State(new int[]{87,80});
    states[80] = new State(new int[]{67,81});
    states[81] = new State(-13);
    states[82] = new State(new int[]{67,83});
    states[83] = new State(new int[]{89,84});
    states[84] = new State(new int[]{67,85});
    states[85] = new State(-14);

    for (int sNo = 0; sNo < states.Length; sNo++) states[sNo].number = sNo;

    rules[1] = new Rule(-2, new int[]{-1,61});
    rules[2] = new Rule(-1, new int[]{-1,-3,59});
    rules[3] = new Rule(-1, new int[]{-1,-4,59});
    rules[4] = new Rule(-1, new int[]{-1,-5,59});
    rules[5] = new Rule(-1, new int[]{});
    rules[6] = new Rule(-3, new int[]{80,78});
    rules[7] = new Rule(-3, new int[]{80,78,81,-6});
    rules[8] = new Rule(-3, new int[]{67,68,78,81,-6});
    rules[9] = new Rule(-3, new int[]{67,69,78,81,-6});
    rules[10] = new Rule(-4, new int[]{82,78,83,64});
    rules[11] = new Rule(-4, new int[]{82,78,83,64,84,64});
    rules[12] = new Rule(-4, new int[]{67,85,78,64});
    rules[13] = new Rule(-5, new int[]{86,67,87,67});
    rules[14] = new Rule(-5, new int[]{88,67,89,67});
    rules[15] = new Rule(-6, new int[]{-7,76,-7});
    rules[16] = new Rule(-6, new int[]{-7,77,-7});
    rules[17] = new Rule(-6, new int[]{-7});
    rules[18] = new Rule(-7, new int[]{});
    rules[19] = new Rule(-7, new int[]{-8});
    rules[20] = new Rule(-7, new int[]{-9});
    rules[21] = new Rule(-7, new int[]{-10});
    rules[22] = new Rule(-7, new int[]{-11});
    rules[23] = new Rule(-8, new int[]{-12,-13,64});
    rules[24] = new Rule(-8, new int[]{-12,-14,64});
    rules[25] = new Rule(-12, new int[]{90});
    rules[26] = new Rule(-12, new int[]{84});
    rules[27] = new Rule(-12, new int[]{91});
    rules[28] = new Rule(-9, new int[]{93,-13,64});
    rules[29] = new Rule(-9, new int[]{93,-14,64});
    rules[30] = new Rule(-10, new int[]{92,68,79});
    rules[31] = new Rule(-10, new int[]{92,69,79});
    rules[32] = new Rule(-11, new int[]{64,-13,62});
    rules[33] = new Rule(-11, new int[]{64,-15,62});
    rules[34] = new Rule(-11, new int[]{64,-13,63});
    rules[35] = new Rule(-11, new int[]{64,-15,63});
    rules[36] = new Rule(-11, new int[]{64,-13,64});
    rules[37] = new Rule(-11, new int[]{64,-14,64});
    rules[38] = new Rule(-11, new int[]{64,-13,65});
    rules[39] = new Rule(-11, new int[]{64,-13,66});
    rules[40] = new Rule(-13, new int[]{68});
    rules[41] = new Rule(-13, new int[]{69});
    rules[42] = new Rule(-15, new int[]{70});
    rules[43] = new Rule(-15, new int[]{71});
    rules[44] = new Rule(-15, new int[]{72});
    rules[45] = new Rule(-15, new int[]{73});
    rules[46] = new Rule(-14, new int[]{74});
    rules[47] = new Rule(-14, new int[]{75});
  }

  protected override void Initialize() {
    this.InitSpecialTokens((int)Tokens.error, (int)Tokens.EOF);
    this.InitStates(states);
    this.InitRules(rules);
    this.InitNonTerminals(nonTerms);
  }

  protected override void DoAction(int action)
  {
#pragma warning disable 162, 1522
    switch (action)
    {
    }
#pragma warning restore 162, 1522
  }

  protected override string TerminalToString(int terminal)
  {
    if (aliasses != null && aliasses.ContainsKey(terminal))
        return aliasses[terminal];
    else if (((Tokens)terminal).ToString() != terminal.ToString(CultureInfo.InvariantCulture))
        return ((Tokens)terminal).ToString();
    else
        return CharToString((char)terminal);
  }

}
}
