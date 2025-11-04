using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class MapBruteforcer
{
    public static int checkGroup(string map, string group)
	{
		int xormask = 0, andmask = 0;
		for (int i = 0; i < group.Length; i++)
		{
			xormask <<= 1;
			andmask <<= 1;
			if (group[i]=='1') xormask++;
			if (group[i]!='*') andmask++;
		}

		int ans = 0;
		for (int i = 0; i < map.Length; i++)
		{
			if (((i ^ xormask) & andmask) == 0)
			{
				if (map[i] == '0') return 0;
				if (map[i] == '1') ans++;
			}
		}
		return ans;
	}
	
	public static string discardGroup(string map, string group)
	{
		int xormask = 0, andmask = 0;
		for (int i = 0; i < group.Length; i++)
		{
			xormask <<= 1;
			andmask <<= 1;
			if (group[i]=='1') xormask++;
			if (group[i]!='*') andmask++;
		}
		char[] ans = map.ToCharArray();
		for (int i = 0; i < map.Length; i++) if (((i ^ xormask) & andmask) == 0 && map[i] == '1') ans[i] = '*';
		StringBuilder str = new StringBuilder();
		foreach (char c in ans)  str.Append(c);
		return str.ToString();
	}

	public static int countOnesInMap(string map)
	{
		int ans = 0;
		for (int i = 0; i < map.Length; i++) if (map[i] == '1') ans++;
		return ans;
	}

	public static int countStarsInGroup(string group)
	{
		int ans = 0;
		for (int i = 0; i < group.Length; i++) if (group[i] == '*') ans++;
		return ans;
	}

	public static string convertNumberToGroup(int number, int length)
	{
		string ans = "";
		for (int i = 0; i < length; i++)
		{
			int z = number % 3;
			number /= 3;
			switch (z)
			{
				case 0: ans = '0' + ans; break;
				case 1: ans = '*' + ans; break;
				case 2: ans = '1' + ans; break;
				default: return "";
			}
		}
		return ans;
	}
	
	public static string bruteForceMap(string map) // Gives a nearly optimized answer for map of any correct size. May take long time to process.
	{
		string mapCopy = string.Copy(map);
		List<string> ans = new List<string>();
		int length3 = (int)Math.Round(Math.Pow(3, Math.Log(map.Length, 2)), MidpointRounding.AwayFromZero);
		int log = (int)Math.Round(Math.Log(map.Length, 2), MidpointRounding.AwayFromZero);
		while (countOnesInMap(mapCopy) > 0)
		{
			int[] bruteForcePoints = new int[length3];
			for (int i = 0; i < length3; i++)
			{
				string group = convertNumberToGroup(i, log);
				bruteForcePoints[i] = checkGroup(mapCopy, group)*(log+1) + countStarsInGroup(group);
				//bruteForcePoints[i] = checkGroup(mapCopy, group)*(log+1) + group.Count(a => a=='*');
			}
			string maxGroup = convertNumberToGroup(bruteForcePoints.IndexOf(a => a == bruteForcePoints.Max()), log);
			ans.Add(maxGroup);
			mapCopy = discardGroup(mapCopy,maxGroup);
		}
		return string.Join("+", ans.ToArray());
	}
}