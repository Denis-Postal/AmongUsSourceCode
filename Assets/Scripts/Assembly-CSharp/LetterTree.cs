using System.Collections.Generic;
using System.Text;

public class LetterTree
{
	private enum NodeTypes : byte
	{
		NonTerm = 0,
		Terminal = 1,
		TerminalStrict = 2,
		TerminalExact = 3,
		TerminalUnbroken = 4
	}

	private class LetterNode
	{
		public char Letter;

		public NodeTypes Terminal;

		public Dictionary<int, LetterNode> Children = new Dictionary<int, LetterNode>();

		public LetterNode(char l)
		{
			Letter = l;
		}

		public LetterNode CreateChild(char l)
		{
			int num = ToIndex(l);
			LetterNode letterNode;
			if (!Children.TryGetValue(num, out letterNode))
			{
				letterNode = new LetterNode(l);
				Children[num] = letterNode;
			}
			return letterNode;
		}

		public LetterNode FindChild(char l)
		{
			int num = ToIndex(l);
			LetterNode letterNode;
			Children.TryGetValue(num, out letterNode);
			return letterNode;
		}

		public static int ToIndex(char c)
		{
			if (c >= 'A' && c <= 'Z')
			{
				return c - 65;
			}
			if (c >= 'a' && c <= 'z')
			{
				return c - 97;
			}
			char lower = char.ToLowerInvariant(c);
			if (lower >= '\u0430' && lower <= '\u044f')
			{
				return 1000 + lower;
			}
			switch (lower)
			{
			case '\u0451':
				return 1000 + '\u0435';
			case '\u0456':
				return 1000 + '\u0438';
			case '\u0457':
				return 1000 + '\u0438';
			case '\u0454':
				return 1000 + '\u0435';
			case '\u0491':
				return 1000 + '\u0433';
			}
			if (c == 'с')
			{
				return 2;
			}
			if (c == 'к')
			{
				return 10;
			}
			if (c == '$')
			{
				return 18;
			}
			if (c == '+')
			{
				return 19;
			}
			if (c == '0')
			{
				return 14;
			}
			if (c == '1')
			{
				return 8;
			}
			if (c == '!')
			{
				return 8;
			}
			if (c == '2')
			{
				return 18;
			}
			if (c == '3')
			{
				return 4;
			}
			if (c == '4')
			{
				return 0;
			}
			if (c == '5')
			{
				return 18;
			}
			if (c == '7')
			{
				return 19;
			}
			if (c == '8')
			{
				return 1;
			}
			if (c > 'z')
			{
				string text = c.ToString().Normalize(NormalizationForm.FormD);
				foreach (char c2 in text)
				{
					if (c2 <= 'z')
					{
						return ToIndex(c2);
					}
				}
			}
			return c;
		}
	}

	private LetterNode root = new LetterNode('\0');

	public void Clear()
	{
		root = new LetterNode('\0');
	}

	public void AddWord(string word)
	{
		LetterNode letterNode = root;
		foreach (char l in word)
		{
			if (!IsFiller(l))
			{
				letterNode = letterNode.CreateChild(l);
			}
		}
		if (letterNode.Terminal == NodeTypes.NonTerm)
		{
			letterNode.Terminal = NodeTypes.Terminal;
			if (word[word.Length - 1] == '~')
			{
				letterNode.Terminal = NodeTypes.TerminalStrict;
			}
			if (word[word.Length - 1] == '^')
			{
				letterNode.Terminal = NodeTypes.TerminalExact;
			}
			if (word[word.Length - 1] == '`')
			{
				letterNode.Terminal = NodeTypes.TerminalUnbroken;
			}
		}
	}

	public bool IsFiller(char l)
	{
		return LetterNode.ToIndex(l) == l;
	}

	public int Search(StringBuilder input, int start)
	{
		if (start >= input.Length || IsFiller(input[start]))
		{
			return 0;
		}
		bool exactStart = start == 0 || IsFiller(input[start - 1]);
		return SubSearchRec(input, start, root, postDupes: false, postBreak: false, exactStart);
	}

	public int Search(string inputStr, int start)
	{
		StringBuilder stringBuilder = new StringBuilder(inputStr);
		if (start >= stringBuilder.Length || IsFiller(stringBuilder[start]))
		{
			return 0;
		}
		bool exactStart = start == 0 || IsFiller(stringBuilder[start - 1]);
		return SubSearchRec(stringBuilder, start, root, postDupes: false, postBreak: false, exactStart);
	}

	private int SubSearchRec(StringBuilder input, int start, LetterNode previous, bool postDupes, bool postBreak, bool exactStart)
	{
		if (start >= input.Length)
		{
			return -2;
		}
		char c = input[start];
		if (IsFiller(c))
		{
			if (postDupes)
			{
				return -2;
			}
			int num = SubSearchRec(input, start + 1, previous, postDupes, postBreak: true, exactStart);
			if (num > 0)
			{
				return num + 1;
			}
			return -2;
		}
		if (c == previous.Letter && !postBreak)
		{
			int num2 = SubSearchRec(input, start + 1, previous, postDupes: true, postBreak, exactStart);
			if (num2 > 0)
			{
				return num2 + 1;
			}
			if (previous.Terminal != NodeTypes.NonTerm)
			{
				return 1;
			}
		}
		LetterNode letterNode = previous.FindChild(c);
		if (letterNode == null)
		{
			return -3;
		}
		int num3 = SubSearchRec(input, start + 1, letterNode, postDupes, postBreak, exactStart);
		if (num3 > 0)
		{
			return num3 + 1;
		}
		if (letterNode.Terminal == NodeTypes.TerminalStrict && num3 == -2 && (exactStart || !postBreak))
		{
			return 1;
		}
		if (letterNode.Terminal == NodeTypes.TerminalUnbroken && num3 == -2 && !postBreak)
		{
			return 1;
		}
		if (letterNode.Terminal == NodeTypes.TerminalExact && num3 == -2 && exactStart)
		{
			return 1;
		}
		if (letterNode.Terminal == NodeTypes.Terminal && num3 <= 0)
		{
			return 1;
		}
		return num3;
	}

	public IEnumerable<string> GetWords()
	{
		StringBuilder b = new StringBuilder();
		foreach (LetterNode node in root.Children.Values)
		{
			foreach (string word in GetWords(b, 0, node))
			{
				yield return word;
			}
		}
	}

	private IEnumerable<string> GetWords(StringBuilder b, int i, LetterNode node)
	{
		if (node == null)
		{
			yield break;
		}
		b.Length++;
		b[i] = node.Letter;
		if (node.Terminal == NodeTypes.Terminal)
		{
			yield return b.ToString();
		}
		else if (node.Terminal == NodeTypes.TerminalStrict)
		{
			b.Length++;
			b[i + 1] = '~';
			yield return b.ToString();
			b.Length--;
		}
		foreach (LetterNode node2 in node.Children.Values)
		{
			foreach (string word in GetWords(b, i + 1, node2))
			{
				yield return word;
			}
		}
		b.Length--;
	}
}
