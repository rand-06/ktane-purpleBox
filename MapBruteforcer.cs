using System.Collections.Generic;
using System.Linq;

public static class MapBruteforcer
{
	private static List<int> getValidDigitsForGroup(int lengthOfMap, string group)
	{
		int xorMask = group.Select(t => t=='1'?1:0).Aggregate(0, (total, next) => 2 * total + next);
		int andMask = group.Select(t => t!='*'?1:0).Aggregate(0, (total, next) => 2 * total + next);
		return Enumerable.Range(0, lengthOfMap).Where(i => ((i ^ xorMask) & andMask) == 0).ToList();
	}
	
	private static int checkGroup(string map, string group) // If group is valid, counts 1s. Else returns 0.
	{
		List<int> validDigits = getValidDigitsForGroup(map.Length, group);
		return validDigits.Any(i => map[i] == '0') ? 0 : validDigits.Count(i=>map[i]=='1');
	}

	private static string discardGroup(this string map, string group) => map.ToCharArray().Select((n, j) => 
		getValidDigitsForGroup(map.Length, group).Where(i => map[i] == '1').ToList().Contains(j) ? '*' : n).Aggregate("", (a, b) => a + b);

	private static string convertNumberToGroup(int number, int length)
	{
		List<int> digits = new List<int>();
		for (int i = 0; i < length; i++)
		{
			digits.Add(number % 3);
			number /= 3;
		}
		return digits.Select(i=>i==0?'0':i==1?'*':'1').Aggregate("", (a,b) => b + a);
	}
	
	public static string bruteForceMap(string map) // Gives a nearly optimized answer for map of any correct size. May take long time to process.
	{
		string mapCopy = string.Copy(map);
		List<string> ans = new List<string>();
		int varCount = 0, possibilities = 1;
		for (int i = map.Length; i > 1; i >>= 1) { varCount++; possibilities *= 3; }
		while (mapCopy.Any(x=>x=='1'))
		{
			int[] points = Enumerable.Range(0, possibilities)
				.Select(i => convertNumberToGroup(i, varCount))
				.Select(group => checkGroup(mapCopy, group) * (varCount + 1) + group.Count(a => a == '*')).ToArray();
			string maxGroup = convertNumberToGroup(points.IndexOf(a => a == points.Max()), varCount); // Gets a group with best score.
			ans.Add(maxGroup); // Adds it to answer.
			mapCopy = mapCopy.discardGroup(maxGroup); // And discards it from map.
		}
		return string.Join("+", ans.ToArray());
	}
}