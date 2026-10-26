using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KModkit;
using UnityEngine;
using Random = UnityEngine.Random;

public class Connection
{
	public int origin;
	public int dest;
	public int input;
	public int output;

	public Connection(int origin = -1, int dest = -1, int input = -1, int output = -1)
	{
		this.origin = origin;
		this.dest = dest;
		this.input = input;
		this.output = output;
	}
}

public class Cell
{
	public int dest;
	public int output;
	public Cell(int dest = -1, int output = -1)
	{
		this.dest = dest;
		this.output = output;
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

	public KMSelectable topSide, bottomSide, leftSide, rightSide, frontSide;

	public GameObject topText, bottomText;
	public Material[] materials;
	public MeshRenderer purpleBox;

	public List<AudioClip> notes;
	public List<AudioClip> tapSounds;
	public AudioClip strikeSound;
	public AudioClip solveSound;

	public Sprite[] sprites; // 0 - awake, 1 - ready, 2 - submit, 3 - solved.
	public KMSelectable status;
	public SpriteRenderer face;
	
	public bool ModuleSolved = false;

	private bool RS;

	private string stackString = "", bitString = "";
	private int timer = 0;

	private string[] answers = new string[3];
	private string[] queries = new string[3];
	private int stage = 0;

	private bool boxAwake = true;

	private int spriteId = 0;
	private bool spritesTurnedOn = false;

	private bool lockInput = false;
	private bool highlightTopBool = false;
	private bool holdingStatus = false;
	private Vector3 bottomTextScale;
	private string SN;
	
	static int ModuleIdCounter;
	int ModuleId;
	private bool tpAutoSolve = false;
	private string[] TPAnswers = new string[3];
	private bool TwitchPlaysActive, TwitchShouldSolve;

	private readonly Dictionary<char, string> conversion = new Dictionary<char, string>
	{
		{ '0', "100" }, { '1', "101" }, { '2', "110" }, { '3', "000" }, { '4', "001" }, { '_', "011" }, { '|', "111" }
	};

	private int getCharFromSN(int index)
	{
		switch (index - 1)
		{
			case 0: case 1: return "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(SN[index-1]);
			case 2: case 5: return SN[index-1] - '0';
			case 3: case 4: return SN[index-1] - 'A' + 1;
			default: return -1;
		}
	}

	int[] inverseDescSort(int[] arr)
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

	string[] transpose(string[] matrix) => (matrix.Length == 0)? new string[0]: 
		Enumerable.Range(0,matrix[0].Length).Select(i => 
			Enumerable.Range(0,matrix.Length).Select(j => matrix[j][i]).Aggregate("",(a,b)=>a+b)).ToArray();

	string flipFlop(string wt, string wt1, char type) => Enumerable.Range(0,wt.Length).Select(i => {
			if (new[]{"01R","11R","00S","10S","00J","11K"}.Contains("" + wt[i] + wt1[i] + type)) return '0';
			if (new[]{"10R","01S","01J","10K"}.Contains("" + wt[i] + wt1[i] + type)) return '1';
			return '*';
		}).Aggregate("",(a,b) => a+b);

	string stars(int length) => Enumerable.Range(0,length).Aggregate("",(a,_) => a+'*');
	
	void logTable(Cell[][] cells, List<string[]> wu, List<string[]> truth, int wIndex, int vIndex, int fIndex)
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
			if (wu[1][i]=="*") continue;
			ans += " " + wu[1][i];
			for (int j = 0; j < cells[i].Length; j++)
				ans += "|" + (cells[i][j].dest == -1 ? (wu[2][0].Length==1?"***:*":"***:**") : wu[0][cells[i][j].dest] + ":" + wu[2][cells[i][j].output]);
			ans += "\n";
		}
		Debug.LogFormat("[Insomnious Automaton #{0}] {1}", ModuleId, ans.Replace("\n","\n[Insomnious Automaton #" + ModuleId +"] "));
		
		//Truth table logging.
		Debug.LogFormat("[Insomnious Automaton #{0}] u + w(t) = w(t+1) : v -> {1}. Truth table:", ModuleId, (RS?"RS":"JK"));
		for (int i = 0; i < truth[0].Length; i++) 
			Debug.LogFormat("[Insomnious Automaton #{0}] {1}+{2} = {3}:{4} -> {5}", ModuleId,
				truth[0][i],truth[1][i],truth[2][i][wIndex],truth[3][i][vIndex],truth[4][i][fIndex]);
	}

	int keepInRange(int num)
	{
		while (num > 5) num -= 3;
		while (num < -5) num += 3;
		return num;
	}

	int[] toTernary(int num) => new[]{num/9%3, num/3%3, num%3};

	int floor(decimal num) =>(int)num;
	int ceil(decimal num) => (int)num + ((num != (int)num)?1:0);
	
	int round(decimal num) => (num - (int)num) < 0.5M ? floor(num) : ceil(num);
	
	int z(int f, int i) // consider optimized 
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

	string toBinary(string val)
	{
		if (val == "TRUE") return "1";
		if (val == "FALSE") return "0";
		return val.ToCharArray().Select(c => conversion[c]).Aggregate((a, b) => a + b);
	}
	
	void fromSNtoSmth()
	{
		int amountOfQ = getCharFromSN(1) % 2 == 0 ? 6 : 5;
		List<List<Connection>> nodes = new List<List<Connection>>();
		for (int i = 0; i < amountOfQ; i++)
		{
			nodes.Add(new List<Connection>());
			int z0 = z(0, i), z1 = z(1, i), z2 = z(2, i);
			if (0 <= i + z0 && i + z0 < amountOfQ)
				nodes[i].Add(new Connection(i,i + z0, 0, i < i + z0 ? 0 : i > i + z0 ? 1 : 2));
			if (0 <= i + z1 && i + z1 < amountOfQ)
				nodes[i].Add(new Connection(i,i + z1, 1, i < i + z1 ? 0 : i > i + z1 ? 1 : 2));
			if (Math.Abs(i + z2) < amountOfQ)
				nodes[i].Add(new Connection(i,Math.Abs(i + z2), 2,
					i < Math.Abs(i + z2) ? 0 : i > Math.Abs(i + z2) ? 1 : 2));
		}

		int[] amountsForEachQ = new int[amountOfQ]; //количества использований по номерам нод
		int[] amountsForEachX = new int[3];
		int[] amountsForEachY = new int[3];
		int[] amountsForEachW = new int[amountOfQ];
		foreach (List<Connection> node in nodes)
		foreach (Connection connection in node)
		{
			amountsForEachW[connection.origin]++;
			amountsForEachQ[connection.dest]++;
			amountsForEachX[connection.input]++;
			amountsForEachY[connection.output]++;
		}

		int[] indicesOfSortedQ = inverseDescSort(amountsForEachQ); //номера нод отсортированные по убыванию
		int[] indicesOfSortedX = inverseDescSort(amountsForEachX);
		int[] indicesOfSortedY = inverseDescSort(amountsForEachY);
		List<List<string>> binaries = new List<List<string>>
		{
			new List<string> { "0", "1" },
			new List<string> { "00", "01", "10", "11" },
			new List<string> { "000", "001", "010", "100", "011", "101", "110", "111" }
		};

		string[] binariesQ = new string[amountOfQ]; //ноды по порядку (0,1,2...), переведенные в бинарки
		string[] binariesX = new string[3];
		string[] binariesY = new string[3];
		int realQAmount = Enumerable.Range(0, amountOfQ).Count(i => amountsForEachQ[i] != 0 || amountsForEachW[i] != 0);
		int realXAmount = amountsForEachX.Count(i => i != 0);
		int realYAmount = amountsForEachY.Count(i => i != 0);
		int lengthQ = realQAmount <= 2 ? 1 : realQAmount <= 4 ? 2 : 3;
		int lengthX = realXAmount <= 2 ? 1 : 2;
		int lengthY = realYAmount <= 2 ? 1 : 2;
		Enumerable.Range(0, amountOfQ).ToList().ForEach(i =>
			binariesQ[indicesOfSortedQ[i]] = (i < realQAmount) ? binaries[lengthQ - 1][i] : stars(lengthQ));
		Enumerable.Range(0, 3).ToList().ForEach(i =>
			binariesX[indicesOfSortedX[i]] = (i < realXAmount) ? binaries[lengthX - 1][i] : stars(lengthX));
		Enumerable.Range(0, 3).ToList().ForEach(i =>
			binariesY[indicesOfSortedY[i]] = (i < realYAmount) ? binaries[lengthY - 1][i] : stars(lengthY));

		Cell[][] table = Enumerable.Range(0, binariesX.Length).Select(_ =>
			Enumerable.Range(0, amountOfQ).Select(__ => new Cell()).ToArray()
		).ToArray();
		for (int i = 0; i < amountOfQ; i++)
			for (int j = 0; j < nodes[i].Count; j++)
				table[nodes[i][j].input][i] = new Cell(nodes[i][j]);
		
		string[] uTable = new string[1 << (lengthX+lengthQ)];
		string[] wTable = new string[1 << (lengthX+lengthQ)];
		string[] w1Table = new string[1 << (lengthX+lengthQ)];
		string[] vTable = new string[1 << (lengthX+lengthQ)];
		string[] flipFlopTable = new string[1 << (lengthX+lengthQ)];
		List<List<string>> binaries2 = new List<List<string>>
		{
			new List<string> { "0", "1" },
			new List<string> { "00", "01", "10", "11" },
			new List<string> { "000", "001", "010", "011", "100", "101", "110", "111" }
		};
		print("lengthX = "+lengthX+"; lengthQ = "+lengthQ+"; realXAmount = "+realXAmount+"; realQAmount = "+realQAmount);
		for (int i = 0; i < 1 << (lengthX+lengthQ); i++)
		{
			uTable[i] = binaries2[lengthX-1][i%(1 << lengthX)];
			wTable[i] = binaries2[lengthQ-1][i/(1 << lengthX)];
			print("i = "+i+"; u= "+uTable[i]+"; w="+wTable[i]);
			int x = Array.IndexOf(binariesX, uTable[i]), y = Array.IndexOf(binariesQ, wTable[i]);
			if (x == -1 || y == -1 || table[x][y].dest == -1)
			{
				w1Table[i] = stars(lengthQ);
				vTable[i] = stars(lengthY);
				flipFlopTable[i] = stars(6);
			}
			else
			{
				w1Table[i] = binariesQ[table[x][y].dest];
				vTable[i] = binariesY[table[x][y].output];
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
		for (int i = 0; i < lengthQ; i++) 
			Debug.LogFormat("[Insomnious Automaton #{0}] w{1}(t+1): {2} is equal to {3} ({4})", ModuleId, i, w1Equations[i], w1Nor[i], toBinary(w1Nor[i]));
		for (int i = 0; i < lengthY; i++)
			Debug.LogFormat("[Insomnious Automaton #{0}] v{1}: {2} is equal to {3} ({4})", ModuleId, i, vEquations[i], vNor[i], toBinary(vNor[i]));
		for (int i = 0; i < lengthQ*2; i++)
			Debug.LogFormat("[Insomnious Automaton #{0}] {1}{2}: {3} is equal to {4} ({5})", ModuleId,
				i >= lengthQ ? (RS ? "S" : "K") : (RS ? "R" : "J"), i%lengthQ, flipFlopEquations[i], flipFlopNor[i], toBinary(flipFlopNor[i]));
		
		int wIndex = Random.Range(0, lengthQ);
		int vIndex = Random.Range(0, lengthY);
		int flipFlopIndex = Random.Range(0, lengthQ*2);
		
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
			new List<string[]>{binariesQ, binariesX, binariesY},
			new List<string[]>{uTable,wTable,w1Table,vTable,flipFlopTable},
			wIndex,vIndex,flipFlopIndex
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

	void solve()
	{
		updateFace(3);
		Audio.HandlePlaySoundAtTransform(solveSound.name, transform);
		ModuleSolved = true;
		bottomText.GetComponent<TextMesh>().text = "Asleep.";
		if (TwitchPlaysActive) TwitchShouldSolve = true;
		else GetComponent<KMBombModule>().HandlePass();
	}

	void updateBottomScreen()
	{
		if (boxAwake) bottomText.GetComponent<TextMesh>().text = "Awake.";
		else if (stackString == "" && bitString == "") bottomText.GetComponent<TextMesh>().text = "Ready.";
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
			if (stage == 3) solve();
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
		if (stage == 3) solve();
		updateTopScreen();
		lockInput = false;
	}

	void sidePress(bool input)
	{
		Audio.HandlePlaySoundAtTransform(tapSounds.PickRandom().name, transform);
		bitString += input?'1':'0';
		checkBits();
	}

	void changeScale() // а хули собсна не работает :(
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
		if (timer == 0) StartCoroutine(strike()); else timer = 0;
	}

	IEnumerator strike()
	{
		if (tpAutoSolve)
		{
			GetComponent<KMBombModule>().HandlePass();
			bottomText.GetComponent<TextMesh>().text = "<color=#ff0000>If you see this,\nreport this to rand06.</color>";
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
		int timer2 = 0;
		while (timer2 < 30 && highlightTopBool)
		{
			timer2++;
			yield return new WaitForSeconds(.1f);
		}
		if (!highlightTopBool) yield break;
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
		int timer2 = 0;
		while (timer2 < 30 && holdingStatus)
		{
			timer2++;
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
	        foreach (var c in Command.Where(c => c != ' '))
	        {
		        sidePress(c=='1');
		        yield return new WaitForSeconds(.1f);
	        }
	        yield break;
        }

        switch (Command.ToLowerInvariant())
        {
	        case "hover":
		        yield return new WaitForSeconds(3f);
		        boxAwake = false;
		        updateBottomScreen();
		        updateFace(1);
		        yield break;
	        case "submit":
		        yield return topPress();
		        //topSide.OnInteract();
		        if (TwitchShouldSolve)
		        {
			        print("Solving the module via yield return solve...");
			        yield return "solve";
			        GetComponent<KMBombModule>().HandlePass();
		        }
		        else print("TwitchShouldSolve is false, proceeding...");
		        yield break;
	        case "clear":
		        bottomSide.OnInteract();
		        yield break;
	        case "face":
		        yield return new WaitForSeconds(3f);
		        spritesTurnedOn = !spritesTurnedOn;
		        updateFace();
		        yield break;
	        default:
		        yield return "sendtochaterror Command is not valid.";
		        break;
        }
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
        GetComponent<KMBombModule>().HandlePass();
    }
}
