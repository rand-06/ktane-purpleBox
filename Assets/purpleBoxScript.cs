using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;
using Random = UnityEngine.Random;

public class Connection
{
	public int dest;
	public int input;
	public int output;

	public Connection(int dest = -1, int input = -1, int output = -1)
	{
		this.dest = dest;
		this.input = input;
		this.output = output;
	}
}

public class Cell
{
	public int dest;
	public int output;
	public Cell(int dest, int output)
	{
		this.dest = dest;
		this.output = output;
	}

	public Cell()
	{
		dest = -1;
		output = -1;
	}

	public Cell(Connection connection)
	{
		dest = connection.dest;
		output = connection.output;
	}
}

public class purpleBoxScript : MonoBehaviour
{

	public KMBombInfo Info;
	public KMAudio Audio;
	
	public KMSelectable topSide;
	public KMSelectable bottomSide;
	public KMSelectable leftSide;
	public KMSelectable rightSide;
	public KMSelectable frontSide;
	
	public GameObject topText;
	public GameObject bottomText;
	public Material[] materials;
	public MeshRenderer purpleBox;
	private bool RS;
	public AudioClip skillCheck;

	private string stackString = "";
	private string bitString = "";
	private int timer = 0;

	private string[] answers = new string[3];
	private string[] queries = new string[3];
	private int stage = 0;
	
	public bool ModuleSolved = false;
	private bool boxAwake = true;
	
	public Sprite[] sprites; // 0 - awake, 1 - ready, 2 - submit, 3 - solved.
	private int spriteId = 0;
	private bool spritesTurnedOn = false;
	
	public KMSelectable status;
	public SpriteRenderer face;

	public AudioClip[] notes;
	public AudioClip strikeSound;
	public AudioClip[] tapSounds;
	public AudioClip solveSound;
	private bool lockInput = false;
	private bool highlightTopBool = false;
	private bool holdingStatus = false;
	private Vector3 bottomTextScale;

	private string SN;
	
	static int ModuleIdCounter;
	int ModuleId;

	private bool tpAutoSolve = false;

	private string[] TPAnswers = new string[3];

	private int getCharFromSN(int index)
	{
		
		string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		switch (index - 1)
		{
			case 0: case 1: return base36.IndexOf(SN[index-1]);
			case 2: case 5: return SN[index-1] - '0';
			case 3: case 4: return SN[index-1] - 'A' + 1;
			default: return -1;
		}
	}

	int[] reverseDescSort(int[] arr)
	{
		string ans = "Usages: ";
		for (int i=0; i<arr.Length; i++) ans += i + ": " + arr[i]+"; ";
		int[] indices = Enumerable.Range(0, arr.Length).ToArray();
		Array.Sort(indices, (a, b) => arr[a] != arr[b] ? arr[b].CompareTo(arr[a]) : a.CompareTo(b));
		ans += "\nAfter sorting: ";
		for (int i=0; i<arr.Length; i++) ans += "[" + indices[i] + "]: " + arr[indices[i]] + "; ";
		Debug.LogFormat("[Insomnious Automaton #{0}] {1}", ModuleId, ans.Replace("\n","\n[Insomnious Automaton #" + ModuleId +"] "));
		return indices;
	}

	string[] transpose(string[] matrix)
	{
		if (matrix.Length == 0) return new string[0];
		string[] ans = new string[matrix[0].Length];
		for (int i = 0; i < matrix[0].Length; i++)
		{
			for (int j = 0; j < matrix.Length; j++)
			{
				ans[i] += matrix[j][i];
			}
		}
		return ans;
	}

	string flipFlop(string wt, string wt1, char type)
	{
		return Enumerable.Range(0,wt.Length).Select(i => {
			if (new[]{"01R","11R","00S","10S","00J","11K"}.Contains("" + wt[i] + wt1[i] + type)) return '0';
			if (new[]{"10R","01S","01J","10K"}.Contains("" + wt[i] + wt1[i] + type)) return '1';
			return '*';
		}.toString()).Aggregate((a,b) => a+b);
		
		string ans = "";
		for (int i = 0; i < wt.Length; i++)
		{
			switch ("" + wt[i] + wt1[i] + type)
			{
				case "01R":
				case "11R":
				case "00S":
				case "10S":
				case "00J":
				case "11K": ans += '0'; break;

				case "10R":
				case "01S":
				case "01J":
				case "10K": ans += '1'; break;
				
				default: ans += '*'; break;
			}
		}
		return ans;
	}

	string stars(int length)
	{
		string ans = "";
		for (int i = 0; i < length; i++) ans += "*";
		return ans;
	}

	string karnaughify(string s)
	{
		int[] indices;
		switch (s.Length)
		{
			case 32: indices = new[]
			{
				0, 4, 12, 8, 24, 28, 20, 16,
				1, 5, 13, 9, 25, 29, 21, 17,
				3, 7, 15, 11, 27, 31, 23, 19,
				2, 6, 14, 10, 26, 30, 22, 18
			}; break;
			case 16: indices = new[]
			{
				0, 4, 12, 8, 
				1, 5, 13, 9, 
				3, 7, 15, 11,
				2, 6, 14, 10
			}; break;
			case 8: indices = new[]
			{
				0, 2, 6, 4, 
				1, 3, 7, 5
			}; break;
			default: indices = new int[0]; break;
		}

		string ans = "";
		foreach (int i in indices) ans += s[i];
		return ans;
	}

	void logTable(Cell[][] cells, List<string[]> wu, List<string[]> truth)
	{
		// QX-table logging.
		string ans = cells[0].Length==6?"x\\q|-0-|-1-|-2-|-3-|-4-|-5-\n":"x\\q|-0-|-1-|-2-|-3-|-4-\n";
		for (int i = 0; i < cells.Length; i++)
		{
			ans += " " + i + " ";
			for (int j = 0; j < cells[i].Length; j++)
				ans += "|" + (cells[i][j].dest == -1 ? "*:*" : cells[i][j].dest + ":" + cells[i][j].output);
			ans += "\n";
		}
		Debug.LogFormat("[Insomnious Automaton #{0}] {1}", ModuleId, ans.Replace("\n","\n[Insomnious Automaton #" + ModuleId +"] "));
		
		//WU-table logging.
		ans = "u\\w";
		for (int i = 0; i < wu[0].Length; i++)
		{
			ans += "|-" + wu[0][i] + "-" + (wu[2][0].Length==1?"":"-");
		}

		ans += '\n';
		for (int i = 0; i < cells.Length; i++)
		{
			ans += " " + wu[1][i];
			for (int j = 0; j < cells[i].Length; j++)
				ans += "|" + (cells[i][j].dest == -1 ? (wu[2][0].Length==1?"***:*":"***:**") : wu[0][cells[i][j].dest] + ":" + wu[2][cells[i][j].output]);
			ans += "\n";
		}
		Debug.LogFormat("[Insomnious Automaton #{0}] {1}", ModuleId, ans.Replace("\n","\n[Insomnious Automaton #" + ModuleId +"] "));
		
		//Truth table logging.
		ans = "u + w(t) = w(t+1) : v -> "+(RS?"RS":"JK")+". Truth table:\n";
		for (int i = 0; i < truth[0].Length; i++)
		{
			ans+=truth[0][i]+"+"+truth[1][i]+" = "+truth[2][i]+":"+truth[3][i]+" -> "+
			     truth[4][i][0]+truth[4][i][3]+" "+
			     truth[4][i][1]+truth[4][i][4]+" "+
			     truth[4][i][2]+truth[4][i][5]+"\n";
		}
		Debug.LogFormat("[Insomnious Automaton #{0}] {1}", ModuleId, ans.Replace("\n","\n[Insomnious Automaton #" + ModuleId +"] "));
		
		
	}

	int keepInRange(int num)
	{
		while (num > 5) num -= 3;
		while (num < -5) num += 3;
		return num;
	}

	int[] toTernary(int num)
	{
		if (num > 26 || num < 0) return new int[0];
		return new[]{num/9, num/3%3, num%3};
	}

	int floor(decimal num)
	{
		return (int)num;
	}

	int ceil(decimal num)
	{
		if (num != (int)num) return (int)num + 1;
		return (int)num;
	}
	
	int round(decimal num)
	{
		decimal nonInt = num - (int)num;
		if (nonInt < 0.5M) return floor(num); return ceil(num);
	}
	
	int z(int f, int i)
	{
		int SN2 = getCharFromSN(2), SN3 = getCharFromSN(3), SN6 = getCharFromSN(6);
		int B = Info.GetBatteryCount(), BH = Info.GetBatteryHolderCount();
		int AA = 2 * B - 2 * BH, D = 2 * BH - B;
		int P = Info.GetPortCount(), PP = Info.GetPortPlateCount(), PT = Info.GetPorts().Distinct().Count();
		int EMP = Info.GetPortPlates().Count(a => a.Length == 0);
		int NOEMP = PP - EMP;
		int DVI = Info.GetPortPlates().Count(a => a.Contains("DVI"));
		int PS = Info.GetPortPlates().Count(a => a.Contains("PS2"));
		int RCA = Info.GetPortPlates().Count(a => a.Contains("StereoRCA"));
		int RJ = Info.GetPortPlates().Count(a => a.Contains("RJ45"));
		int PAR = Info.GetPortPlates().Count(a => a.Contains("Parallel"));
		int SER = Info.GetPortPlates().Count(a => a.Contains("Serial"));
		int I = Info.GetIndicators().Count();
		int LI = Info.GetOnIndicators().Count();
		int UI = Info.GetOffIndicators().Count();
		int SND = Info.GetIndicators().Contains("SND")?1:0;
		int CLR = Info.GetIndicators().Contains("CLR")?1:0;
		int CAR = Info.GetIndicators().Contains("CAR")?1:0;
		int IND = Info.GetIndicators().Contains("IND")?1:0;
		int FRQ = Info.GetIndicators().Contains("FRQ")?1:0;
		int SIG = Info.GetIndicators().Contains("SIG")?1:0;
		int NSA = Info.GetIndicators().Contains("NSA")?1:0;
		int MSA = Info.GetIndicators().Contains("MSA")?1:0;
		int TRN = Info.GetIndicators().Contains("TRN")?1:0;
		int BOB = Info.GetIndicators().Contains("BOB")?1:0;
		int FRK = Info.GetIndicators().Contains("FRK")?1:0;


		int ans;
		switch (9 * f + 3 * toTernary(getCharFromSN(5))[f] + toTernary(getCharFromSN(4))[f])
		{
			case 0: ans = SN6 - 5        ; break;
			case 1: ans = AA - D         ; break;
			case 2: ans = LI - UI        ; break;
			case 3: ans = EMP - NOEMP    ; break;
			case 4: ans = 3 - B          ; break;
			case 5: ans = 2 * PP - 3     ; break;
			case 6: ans = BH - I         ; break;
			case 7: ans = PT - 3         ; break;
			case 8: ans = 2 * P - SN3 - 2; break;
			
			case 9+0: ans = SN3 - 2 * i                            ; break;
			case 9+1: ans = 3 * B - D - 2 * i                      ; break;
			case 9+2: ans = 4 + FRK + TRN - BOB - SND - 2 * i      ; break;
			case 9+3: ans = 2 + PAR + SER - 2 * i                  ; break;
			case 9+4: ans = floor(SN2 / 4M) - 2 * i                ; break;
			case 9+5: ans = NOEMP + I - 2 * i                      ; break;
			case 9+6: ans = 4 + P - LI - UI - 2 * i                ; break;
			case 9+7: ans = PT + AA - EMP - 2 * i                  ; break;
			case 9+8: ans = (SN6 % 4) + BH + floor(PP / 2M) - 2 * i; break;
			
			case 18+0: ans = round((SN2 - SN3 - SN6) / 3M - (1 + EMP) * i / (decimal)(1 + BH)); break;
			case 18+1: ans = 2 + B - floor((D + 7) * i / (decimal)(3 + UI))                   ; break;
			case 18+2: ans = 6 - I - 2 * i * (BOB + SIG + MSA + IND)                          ; break;
			case 18+3: ans = 2 + PP - ceil(i * PT / 2M)                                       ; break;
			case 18+4: ans = 1 + AA - i * (I - CLR - CAR - FRQ - NSA)                         ; break;
			case 18+5: ans = LI - i * (1 + RJ + PS + RCA - DVI)                               ; break;
			case 18+6: ans = 3 + UI - i * (round(2 * (5 - SN3) / 3M) + BOB)                   ; break;
			case 18+7: ans = 4 - i * (1 + floor((1 + P) / (1 + (decimal)(SN2 % 3))))          ; break;
			case 18+8: ans = 3 + ceil(round(SN6 / 2M) / 2M) - i * (2 + ceil(NOEMP / 2M))      ; break;
			
			default: return int.MinValue;
		}

		return keepInRange(ans);
	}
	
	void fromSNtoSmth()
	{
		int amountOfNodes = getCharFromSN(1) % 2 == 0 ? 6 : 5;
		
		List<List<Connection>> nodes = new List<List<Connection>>();
		for (int i = 0; i < amountOfNodes; i++)
		{
			nodes.Add(new List<Connection>());
		}

		for (int i = 0; i < amountOfNodes; i++)
		{
			int z0 = z(0, i),  z1 = z(1, i), z2 = z(2, i);
			if (0<=i+z0 && i+z0<amountOfNodes)
				nodes[i].Add(new Connection(i+z0, 0, i<i+z0?0:i>i+z0?1:2));
			if (0<=i+z1 && i+z1<amountOfNodes)
				nodes[i].Add(new Connection(i+z1, 1, i<i+z1?0:i>i+z1?1:2));
			if (Math.Abs(i+z2)<amountOfNodes)
				nodes[i].Add(new Connection(Math.Abs(i+z2), 2, i<Math.Abs(i+z2)?0:i>Math.Abs(i+z2)?1:2));
		}
		
		int[] amountsNodes = new int[amountOfNodes];	//количества использований по номерам нод
		int[] amountsI = new int[3];
		int[] amountsO = new int[3];
		foreach (List<Connection> node in nodes)
		{
			foreach (Connection c in node)
			{
				amountsNodes[c.dest]++;
				amountsI[c.input]++;
				amountsO[c.output]++;
			}
		}
		
		int[] indexNodes = reverseDescSort(amountsNodes);	//номера нод отсортированные по убыванию
		int[] indexI = reverseDescSort(amountsI);
		int[] indexO = reverseDescSort(amountsO);
		List<List<string>> binaries = new List<List<string>>
		{
			new List<string> { "0", "1" },
			new List<string> { "00", "01", "10", "11" },
			new List<string> { "000", "001", "010", "100", "011", "101", "110", "111" }
		};
		
		string[] binariesString = new string[amountOfNodes];			//ноды по порядку (0,1,2...), переведенные в бинарки
		string[] binariesI = new string[3];
		string[] binariesO = new string[3];

		for (int i = 0; i < amountOfNodes; i++)
		{
			binariesString[indexNodes[i]] = binaries[2][i];
		}

		for (int i = 0; i < binariesI.Length; i++)
		{
			binariesI[indexI[i]] = (i>=binaries[amountsI.Contains(0) ? 0 : 1].Count)?"*":binaries[amountsI.Contains(0) ? 0 : 1][i];
		}
		for (int i = 0; i < binariesO.Length; i++)
		{
			binariesO[indexO[i]] = (i>=binaries[amountsO.Contains(0) ? 0 : 1].Count)?"*":binaries[amountsO.Contains(0) ? 0 : 1][i];
		}

		Cell[][] table = new Cell[binariesI.Length][];
		for (int i = 0; i < binariesI.Length; i++)
		{
			table[i] = new Cell[amountOfNodes];
			for (int j = 0; j < amountOfNodes; j++) table[i][j] = new Cell();
		}

		for (int i = 0; i < amountOfNodes; i++)
		{
			for (int j = 0; j < nodes[i].Count; j++)
			{
				table[nodes[i][j].input][i] = new Cell(nodes[i][j]);
			}
		}
		string[] uTable = new string[1 << (amountsI.Contains(0) ? 4 : 5)];
		string[] wTable = new string[1 << (amountsI.Contains(0) ? 4 : 5)];
		string[] w1Table = new string[1 << (amountsI.Contains(0) ? 4 : 5)];
		string[] vTable = new string[1 << (amountsI.Contains(0) ? 4 : 5)];
		string[] flipFlopTable = new string[1 << (amountsI.Contains(0) ? 4 : 5)];
		string[] binaries2 = {"000","001","010","011","100","101","110","111"};
		for (int i = 0; i < uTable.Length; i++)
		{
			if (amountsI.Contains(0))		//только 2 входа
			{
				uTable[i] = binaries[0][i%2];
				wTable[i] = binaries2[i/2];
			}
			else				//3 входа.
			{
				uTable[i] = binaries[1][i%4];
				wTable[i] = binaries2[i/4];
			}

			int x = Array.IndexOf(binariesI, uTable[i]), y = Array.IndexOf(binariesString, wTable[i]);
			if (x == -1 || y == -1 || table[x][y].dest == -1)
			{
				w1Table[i] = "***";
				vTable[i] = stars(amountsO.Contains(0) ? 1 : 2);
				flipFlopTable[i] = "******";
			}
			else
			{
				w1Table[i] = binariesString[table[x][y].dest];
				vTable[i] = binariesO[table[x][y].output];
				flipFlopTable[i] = flipFlop(wTable[i], w1Table[i], RS ? 'R' : 'J') +
				                   flipFlop(wTable[i], w1Table[i], RS ? 'S' : 'K');
			}
		}
		
		string[] w1Maps = transpose(w1Table);
		string[] vMaps =  transpose(vTable);
		string[] flipFlopMaps = transpose(flipFlopTable);
		
		string[] w1Equations = Array.ConvertAll(w1Maps, MapBruteforcer.bruteForceMap);
		string[] vEquations = Array.ConvertAll(vMaps, MapBruteforcer.bruteForceMap);
		string[] flipFlopEquations = Array.ConvertAll(flipFlopMaps, MapBruteforcer.bruteForceMap);
		string[] w1Nor = Array.ConvertAll(w1Equations,OperatorDancer.convertEquToNor);
		string[] vNor = Array.ConvertAll(vEquations,OperatorDancer.convertEquToNor);
		string[] flipFlopNor = Array.ConvertAll(flipFlopEquations,OperatorDancer.convertEquToNor);
		
		Debug.LogFormat(@"[Insomnious Automaton #{24}] w0(t+1): {0} is equal to {1}
[Insomnious Automaton #{24}] w1(t+1): {2} is equal to {3}
[Insomnious Automaton #{24}] w2(t+1): {4} is equal to {5}
[Insomnious Automaton #{24}] 
[Insomnious Automaton #{24}] v0: {6} is equal to {7}
[Insomnious Automaton #{24}] v1: {8} is equal to {9}
[Insomnious Automaton #{24}] 
[Insomnious Automaton #{24}] {10}0: {11} is equal to {12}
[Insomnious Automaton #{24}] {13}0: {14} is equal to {15}
[Insomnious Automaton #{24}] {10}1: {16} is equal to {17}
[Insomnious Automaton #{24}] {13}1: {18} is equal to {19}
[Insomnious Automaton #{24}] {10}2: {20} is equal to {21}
[Insomnious Automaton #{24}] {13}2: {22} is equal to {23}", 
			w1Equations[0], w1Nor[0],
			w1Equations[1], w1Nor[1],
			w1Equations[2], w1Nor[2],
			vEquations[0], vNor[0],
			vEquations.Length>1?vEquations[1]:"-", vEquations.Length>1?vNor[1]:"-",
			RS?"R":"J",
			flipFlopEquations[0], flipFlopNor[0],
			RS?"S":"K",
			flipFlopEquations[3], flipFlopNor[3],
			flipFlopEquations[1], flipFlopNor[1],
			flipFlopEquations[4], flipFlopNor[4],
			flipFlopEquations[2], flipFlopNor[2],
			flipFlopEquations[5], flipFlopNor[5], ModuleId
			);
			
			
		
		int wIndex = Random.Range(0, w1Equations.Length);
		int vIndex = Random.Range(0, vEquations.Length);
		int flipFlopIndex = Random.Range(0, flipFlopMaps.Length);
		
		answers[0] = w1Maps[wIndex];
		answers[1] = vMaps[vIndex];
		answers[2] = flipFlopMaps[flipFlopIndex];
		TPAnswers[0] = w1Nor[wIndex];
		TPAnswers[1] = vNor[vIndex];
		TPAnswers[2] = flipFlopNor[flipFlopIndex];
		
		queries[0] = "w" + wIndex;
		queries[1] = "v" + vIndex;
		queries[2] = flipFlopIndex>2?("F"+(flipFlopIndex-3)):("f"+flipFlopIndex);
		updateTopScreen();
		updateBottomScreen();
		logTable(
			table, 
			new List<string[]>{binariesString, binariesI, binariesO},
			new List<string[]>{uTable,wTable,w1Table,vTable,flipFlopTable}
			);
		
	}

	void updateTopScreen()
	{
		switch (stage)
		{
			case 0: topText.GetComponent<TextMesh>().text = queries[0] + " \t " + queries[1] + " \t " + queries[2]; return;
			case 1: topText.GetComponent<TextMesh>().text = "<color=#00ff00>" + queries[0] + "</color> \t " + queries[1] + " \t " + queries[2]; return;
			case 2: topText.GetComponent<TextMesh>().text = "<color=#00ff00>" + queries[0] + " \t " + queries[1] + "</color> \t " + queries[2]; return;
			case 3: topText.GetComponent<TextMesh>().text="<color=#00ff00>SOLVED!</color>"; return;
			default: return;
		}
	}
	
	
	void checkBits()
	{
		if (bitString.Length == 3)
		{
			switch (bitString)
			{
				case "000": stackString += '3'; break;
				case "001": stackString += '4'; break;
				//case "010": Audio.HandlePlaySoundAtTransform(skillCheck.name, transform); break;
				case "011": stackString += '-'; break;
				case "100": stackString += '0'; break;
				case "101": stackString += '1'; break;
				case "110": stackString += '2'; break;
				case "111": stackString += '!'; break;
			}
			bitString = "";
		}
		updateBottomScreen();		
	}

	IEnumerator solve()
	{
		updateFace(3);
		Audio.HandlePlaySoundAtTransform(solveSound.name, transform);
		ModuleSolved = true;
		bottomText.GetComponent<TextMesh>().text = "Asleep.";
		GetComponent<KMBombModule>().HandlePass();
		yield return null;
	}

	void updateBottomScreen()
	{
		if (boxAwake) bottomText.GetComponent<TextMesh>().text = "Awake.";
		else if (stackString == "" && bitString == "")
		{
			bottomText.GetComponent<TextMesh>().text = "Ready.";
		}
		else bottomText.GetComponent<TextMesh>().text = stackString+"\n"+bitString;
		changeScale();
	}
	
	IEnumerator topPress()
	{
		lockInput = true;
		if (stackString == "")
		{
			if ((bitString == "0" && !answers[stage].Contains('1')) || (bitString == "1" && !answers[stage].Contains('0')))
			{
				stage++;
				bitString = "";
			}
			else
			{
				StartCoroutine(strike());
			}
			updateBottomScreen();
			if (stage == 3) StartCoroutine(solve());
			updateTopScreen();
			yield break;
		}

		string noteChars = "01234-!";
		updateFace(2);
		TextMesh bottomTextMesh = bottomText.GetComponent<TextMesh>();
		for (int i = 0; i < stackString.Length; i++)
		{
			bottomTextMesh.text = "<color=#ffff00>" + stackString.Substring(0,i+1) +  "</color>" + stackString.Substring(i+1) + "\nSubmitting...";
			Audio.HandlePlaySoundAtTransform(notes[noteChars.IndexOf(stackString[i])].name, transform);
			yield return new WaitForSeconds(.5f);
		}
		yield return new WaitForSeconds(2f);
		if (
			OperatorDancer.strictXnor(OperatorDancer.norToTruthTable(stackString.Replace('!', '|').Replace('-', '_'),
				(int)Math.Log(answers[stage].Length, 2)),answers[stage], ModuleId)
		)
		{
			stage++;
			bitString = "";
			stackString = "";
			updateFace(1);
		}
		else
		{
			StartCoroutine(strike());
		}
		updateBottomScreen();
		if (stage == 3) StartCoroutine(solve());
		updateTopScreen();
		lockInput = false;
	}

	void sidePress(bool input)
	{
		Audio.HandlePlaySoundAtTransform(tapSounds[Random.Range(0, tapSounds.Length)].name, transform);
		bitString += input?'1':'0';
		checkBits();
	}

	void changeScale()
	{
		bottomText.transform.localScale = bottomTextScale;
		Bounds textBounds = bottomText.GetComponent<Renderer>().bounds;
		if (textBounds.size.x > .10f)
			bottomText.transform.localScale = new Vector3(
				bottomTextScale.x * (.10f / textBounds.size.x),
				bottomTextScale.y,
				bottomTextScale.z
			);
	}

	void bottomPress()
	{
		stackString = "";
		bitString = "";
		updateBottomScreen();
	}

	void frontPress()
	{
		if (stackString.Length>0) stackString = stackString.Substring(0, stackString.Length - 1);
		updateBottomScreen();
		if (timer == 0)
		{
			StartCoroutine(strike());
		}
		else
		{
			timer = 0;
		}
	}

	IEnumerator strike()
	{
		if (tpAutoSolve)
		{
			GetComponent<KMBombModule>().HandlePass();
			bottomText.GetComponent<TextMesh>().text = "<color=#ff0000>If you see this,\nreport this to rand06 IMMEDIATELY.</color>";
			topText.GetComponent<TextMesh>().text = "<color=#ff0000>BUG!</color>";
			ModuleSolved = true;
			stage = 3;
			yield break;
		}
		Audio.HandlePlaySoundAtTransform(strikeSound.name, transform);
		GetComponent<KMBombModule>().HandleStrike();
		boxAwake = true;
		updateFace(0);
		updateBottomScreen();
		lockInput = false;
		yield return null;
	}

	IEnumerator timerInc()
	{
		while (!ModuleSolved)
		{
			yield return new WaitForSeconds(.2f);
			timer++;
		}
	}

	IEnumerator highlightTop()
	{
		highlightTopBool = true;
		int timer = 0;
		while (timer < 30 && highlightTopBool)
		{
			timer++;
			yield return new WaitForSeconds(.1f);
		}

		if (!highlightTopBool) 
			yield break;
		boxAwake = false;
		updateBottomScreen();
		updateFace(1);
	}

	void updateFace(int newId = -1)
	{
		if (newId != -1) spriteId = newId;
		face.sprite = spritesTurnedOn ? sprites[spriteId] : null;
	}

	IEnumerator turnOnSprites()
	{
		holdingStatus = true;
		int timer = 0;
		while (timer < 30 && holdingStatus)
		{
			timer++;
			yield return new WaitForSeconds(.1f);
		}
		if (!holdingStatus) yield break;
		spritesTurnedOn = !spritesTurnedOn;
		updateFace();
	}
	
	void Start () {
		ModuleId = ModuleIdCounter++;
		ModuleId++;
		SN = Info.GetSerialNumber();
		RS = Random.value < 0.5f;
		purpleBox.material = materials[RS ? 1 : 0];
		bottomTextScale = bottomText.transform.localScale;
		StartCoroutine(timerInc());
		fromSNtoSmth();

		topSide.OnInteract += delegate { if (!ModuleSolved && !boxAwake && !lockInput) StartCoroutine(topPress()); return false; };
		leftSide.OnInteract += delegate { if (!ModuleSolved && !boxAwake && !lockInput) sidePress(false); return false; };
		rightSide.OnInteract += delegate { if (!ModuleSolved && !boxAwake && !lockInput) sidePress(true); return false; };
		bottomSide.OnInteract += delegate { if (!ModuleSolved && !boxAwake && !lockInput) bottomPress(); return false; };
		frontSide.OnInteract += delegate { if (!ModuleSolved && !boxAwake && !lockInput) frontPress(); return false; };
		topSide.OnHighlight += delegate { if (!ModuleSolved && boxAwake) StartCoroutine(highlightTop()); };
		topSide.OnHighlightEnded += delegate { if (!ModuleSolved) highlightTopBool = false; };
		status.OnInteract += delegate { if (!ModuleSolved) StartCoroutine(turnOnSprites()); return false; };
		status.OnInteractEnded += delegate { holdingStatus = false; };

		updateTopScreen();
	}

	#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} hover to hover over box' lid. Use !{0} 0/1 to input 0 or 1; you can use spaces, for example !{0} 100 011 101 011 111 011. Use !{0} submit to submit a stage. Use !{0} clear to clear the input. Use !{0} face to toggle additional state indication.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
        updateTopScreen();
        if (Command.RegexMatch("^[01 ]+$"))
        {
	        foreach (var c in Command)
		        if (c != ' ')
		        {
			        sidePress(c=='1');
			        yield return new WaitForSeconds(.1f);
		        }
	        yield break;
        }

        if (Command.ToLowerInvariant() == "hover")
        {
	        yield return new WaitForSeconds(3f);
	        boxAwake = false;
	        updateBottomScreen();
	        updateFace(1);
	        yield break;
        }

        if (Command.ToLowerInvariant() == "submit")
        {
	        topSide.OnInteract();
	        yield break;
        }

        if (Command.ToLowerInvariant() == "clear")
        {
	        bottomSide.OnInteract();
	        yield break;
        }

        if (Command.ToLowerInvariant() == "face")
        {
	        yield return new WaitForSeconds(3f);
	        spritesTurnedOn = !spritesTurnedOn;
	        updateFace();
	        yield break;
        }

        yield return "sendtochaterror Command is not valid.";
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        tpAutoSolve = true;
        yield return ProcessTwitchCommand("hover");
        bottomSide.OnInteract();
        while (stage<3)
        {
	        string bitAnswer = "";
	        foreach (var c in TPAnswers[stage])
	        {
		        switch (c)
		        {
			        case '0': bitAnswer += "100"; break;
			        case '1': bitAnswer += "101"; break;
			        case '2': bitAnswer += "110"; break;
			        case '3': bitAnswer += "000"; break;
			        case '4': bitAnswer += "001"; break;
			        case '_': bitAnswer += "011"; break;
			        case '|': bitAnswer += "111"; break;
		        }
	        }

	        foreach (var c in bitAnswer)
	        {
		        sidePress(c=='1');
		        yield return new WaitForSeconds(.1f);
	        }
			yield return topPress();
	        //while (lockInput) yield return new WaitForSeconds(.5f);
        }
    }
}
