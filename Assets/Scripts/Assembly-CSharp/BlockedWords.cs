using System.Collections.Generic;
using System.Text;

public static class BlockedWords
{
	private class LengthCompare : IComparer<string>
	{
		public static readonly LengthCompare Instance = new LengthCompare();

		public int Compare(string x, string y)
		{
			return -x.Length.CompareTo(y.Length);
		}
	}

	public static readonly HashSet<char> SymbolChars;

	private static readonly LetterTree SkipList;

	private const char CensorChar = '*';

	private const string CensorTmpFontName = "LiberationSans SDF";

	public static readonly string[] AllWords;

	static BlockedWords()
	{
		SymbolChars = new HashSet<char>
		{
			'?', '!', ',', '.', '\'', ':', ';', '(', ')', '/',
			'\\', '%', '^', '&', '-', '='
		};
		AllWords = new string[]
		{
			"anal~", "analingus", "anilingus", "anus^", "apeshit", "areola", "areole", "arian~", "arse~", "arsehole",
			"aryan~", "ass^", "assbandit", "assbang", "assbite", "assclown", "asscock", "asscracker", "asses~", "assface",
			"assfuck", "assfukka", "assgoblin", "asshat", "asshead", "assho1e", "asshole", "asshopper", "assjabber", "assjacker",
			"asslick", "assmaster", "assmonkey", "assmucus", "assmunch", "assnigger", "asspirate", "asswad", "asswhole", "asswipe",
			"autoerotic", "axwound", "azazel", "azz", "babybatter", "babyjuice", "ballbag", "ballgag", "ballgravy", "ballkicking",
			"balllicking", "balls^", "ballsac", "ballsak", "bampot", "bangbros", "bareback", "barelylegal", "barenaked", "bastard",
			"bastinado", "battyboy", "bawdy", "bbw", "bdsm", "beaner", "beardedclam", "beastial", "beatch", "beaver",
			"beefcurtain", "beeyotch", "bellend", "beotch", "bescumber", "bestial", "b!atch", "b!gblack", "b!gbreasts", "b!gknockers",
			"b!gtits", "b!mbo", "b!nt", "b!rdlock", "b!tch", "bisexual", "blackcock", "blondeaction", "bloodclaat", "bloodyhell",
			"blowjob", "blowme", "blowmud", "blowyourload", "bluewaffle", "blumpkin", "b0iolas", "b0llock", "b0llok", "b0llox",
			"b0ndage", "b0ned", "b0ner", "b0ng", "b00b", "b00ger", "b00kie", "b00tee", "b00tie", "b00ty",
			"b00ze", "b00zy", "b0som", "breasts", "breeder", "brotherfucker", "brownshowers", "brunetteaction", "buceta", "bukkake",
			"bulldyke", "bulletvibe", "bullshit", "bullturds", "buncombe", "bung", "bunnyfucker", "bustaload", "busty", "buttcheeks",
			"buttfuck", "butthole", "buttmuch", "buttmunch", "buttpirate", "buttplug", "caca", "cahone", "cameltoe", "camgirl",
			"camslut", "camwhore", "carpetmuncher", "cawk", "cervix", "chesticle", "chichiman", "chickwithadick", "childfucker", "chinc",
			"chink", "choad", "chocice", "chocolaterosebuds", "chode", "chotabags", "cipa~", "circlejerk", "clevelandsteamer", "cl1max",
			"cl1t", "cloverclamps", "clunge", "clusterfuck", "cnut", "c0cain", "c0ccydynia", "c0ck", "c0ffindodger", "c0ital",
			"c0k", "c0mmie", "c0ndom", "c0ochie", "c0ochy", "c0on", "c0oter", "c0prolagnia", "c0prophilia", "c0psomewood",
			"c0rksucker", "c0rnhole", "c0rpulent", "c0rpwhore", "c0x", "crack", "creampie", "cretin", "crikey", "cripple",
			"crotte", "cum~", "cumbubble", "cumchugger", "cumdump", "cumfreak", "cumguzzler", "cumjockey", "cums~", "cumtart",
			"cunilingus", "cunnie", "cunny", "cunt", "cutrope", "cyalis", "cyberfuc", "dago", "darkie", "daterape",
			"dawgiestyle", "deepthroat", "deggo", "dendrophilia", "dick", "diddle", "dike", "dild0", "diligaf", "dillweed",
			"dimwit", "dingle", "dink", "dipship", "dipshit", "dirsa", "dirtypillows", "dirtysanchez", "dlck", "d0gfucker",
			"d0ggiestyle", "d0ggin", "d0ggystyle", "d0gstyle", "d0lcett", "d0mination", "d0minatrix", "d0mmes", "d0ng", "d0nkeypunch",
			"d0nkeyribber", "d0ochbag", "d0ofus", "d0okie", "d0osh", "d0pey", "d0ubledong", "d0ublelift", "d0ublepenetration", "d0uch3",
			"dpaction", "drilldo", "dryhump", "duche", "dumass", "dumbass", "dumbcunt", "dumbfuck", "dumbshit", "dummy",
			"dumshit", "dvda", "dyke", "eatadick", "eathairpie", "eatmyass", "ecchi", "ejaculate", "ejaculating", "ejaculation",
			"ejakulate", "erect", "erotic", "erotism", "essohbee", "eunuch", "extacy", "extasy", "f4cial", "f4ck",
			"f4g", "f4ig", "f4nny", "f4nyy", "f4tass", "fcuk", "fecal", "feck", "feist", "felch",
			"fellate", "fellatio", "feltch", "femalesquirting", "femdom", "fenian", "figging", "fingerbang", "fingerfuck", "fingering",
			"fisted", "fistfuck", "fisting", "fisty", "flange", "flaps", "fleshflute", "flogthelog", "floozy", "foad",
			"foah", "fondle", "foobar", "fook", "footfetish", "footjob", "foreskin", "freex", "frenchify", "frigg",
			"frotting", "fubar", "fuc", "fudgepacker", "fuk", "fuq", "furfag", "furryfag", "futanari", "fuck",
			"fux", "fvck", "fxck", "gangbang", "ganja", "gash", "gassyass", "gay", "genderbender", "genitals",
			"gey~", "gfy", "ghay", "ghey", "giantcock", "gigolo", "gippo", "girlon", "girlsgonewild", "glans",
			"goatcx", "goatse", "gokkun", "goldenshower", "golliwog", "gonad", "gooch", "googirl", "gook", "goregasm",
			"gringo", "grope", "groupsex", "gspot", "guido", "guro", "hamflap", "handjob", "hardon", "hell~",
			"hells", "hemp", "homo^", "hentai", "heroin", "heshe~", "hircismus", "hitler", "hugefat", "hump~",
			"hussy", "hymen", "inbred", "incest", "injun", "intercourse", "jackass", "jackhole", "jackoff", "jaggi",
			"jagoff", "jailbait", "jellydonut", "jigaboo", "jiggerboo", "jism", "jiz", "jock", "juggs", "junglebunny",
			"junkie", "junky", "kafir", "kawk", "kike", "kinbaku", "kinkster", "kinky", "klan", "kock",
			"kondum", "kooch", "kootch", "kraut", "kum", "kunilingus", "kunja", "kunt~", "kwif", "kyke",
			"labia", "lameass", "lardass", "l3i+ch", "l3monparty", "l3per", "l3sbian", "l3sbo~", "l3z~", "lolita",
			"looney", "lovemaking", "lube", "lust", "m4fugly", "m4kemecome", "m4lesquirting", "m4ms", "m45ochist", "m45sa",
			"m45terbate", "m45terbating", "m45terbation", "m45terb8", "m45turbate", "m45turbating", "m45turbation", "mcfagget", "menageatrois", "menses",
			"menstruate", "menstruation", "meth~", "mfucking", "mick", "microphallus", "middlefinger", "midget", "milf", "minge",
			"missionaryposition", "m0f0", "m0lest", "m0olie", "m0omoofoofoo", "m0ron", "m0thafuck", "m0therfuck", "m0undofvenus", "mrhands",
			"muff", "munging", "munter", "mutha", "muther", "naked", "nambla", "napalm", "nappy", "nawashi",
			"nazi", "ngga^", "nggr", "nlgger", "nlgga", "nlggr", "n1gaboo", "n1qqa", "n1qqer", "n1gga",
			"n1bba", "n1ggr", "n1gger", "n1ggle", "n1glet", "n1gnog", "n1mphomania", "n1mrod", "n1nny", "n1pple",
			"nonce", "nsfwimages", "nude", "nudity", "numbnuts", "nutbutter", "nutsack", "nutter", "nympho", "octopussy",
			"oldbag", "omorashi", "onecuptwogirls", "oneguyonejar", "opiate", "opium", "orally", "orgasim", "orgasm", "orgies",
			"orgy", "ovary", "ovum", "paedophile", "paki~", "panooch", "pansy", "pantie", "panty", "pecker",
			"pedo~", "pedophile", "pegging", "penetrate", "penetration", "penial", "penile", "penis", "perversion", "phallic",
			"phonesex", "phuck", "phuk", "phuq", "pigfucker", "pikey", "pillowbiter", "pimp", "pinko~", "playboy",
			"pleasurechest", "p0lack", "p0lesmoker", "p0llock", "p0nyplay", "p0on", "p0rchmonkey", "p0rn", "prick", "prig",
			"princealbertpiercing", "pron", "prostitute", "prude", "psycho", "pthc", "pube", "pubic", "pubis", "punani",
			"punkass", "punky", "punta", "puss", "puta~", "puto~", "queaf", "queef", "queer", "quicky",
			"quim", "racy", "raghead", "ragingboner", "rape~", "raper~", "raping", "rapist", "ratard", "raunch",
			"rectal", "rectum", "rectus", "reefer", "reich", "renob", "retard", "reversecowgirl", "revue", "rimjaw",
			"rimjob", "rimming", "ritard", "rosypalm", "rtard", "rubbish", "rump", "ruski", "rustytrombone", "sadism",
			"sadist", "sambo", "sandbar", "sandler", "sandnigger", "sanger", "santorum", "sausagequeen", "scag", "scantily",
			"scat", "schizo", "schlong", "scissoring", "scroat", "scrog", "scrot", "scrud", "seaman", "seamen",
			"seks", "semen", "sex", "shag", "shamedame", "shavedbeaver", "shavedpussy", "shemale", "shibari", "shirtlifter",
			"shit^", "shiz", "shota", "shrimping", "sissy", "skag", "skank", "skeet", "skullfuck", "slag",
			"slanteye", "slave", "sleaze", "sleazy", "slope", "slut", "smartass", "smut", "snatch", "snowballing",
			"snuff", "s0doff", "s0dom", "s0nofabitch", "s0nofawhore", "spade", "sperm", "spic~", "spick", "spik~",
			"splooge", "spooge", "spreadlegs", "spunk", "stfu", "stiffy", "strapon", "strappado", "styledoggy", "suckass",
			"suckingass", "suicidegirls", "sultrywomen", "sumofabiatch", "swastika", "swinger", "taintedlove", "tampon", "tard", "tastemy",
			"tawdry", "teabagging", "teat", "teets", "teez", "teste~", "testes", "testical", "testicle", "testis",
			"threesome", "throating", "thundercunt", "thot~", "t1edup", "t1ghtwhite", "t1nkle", "t1t`", "t1ts`", "t1tty",
			"t1ttie", "tongueina", "toots", "topless", "tosser", "towelhead", "tramp", "tranny", "transsexual", "trashy",
			"tribadism", "tubgirl", "turd", "tush", "tw4t^", "twink", "twofingers", "twogirlsonecup", "twunt", "unclefucker",
			"undies", "undressing", "upskirt", "urethraplay", "urinal", "urine", "urophilia", "uterus", "vajayjay", "vajj",
			"valium", "venusmound", "veqtable", "v14gra", "v1brator", "v1gra", "v1oletwand", "v1rgin", "v1xen", "vjayjay",
			"vodka", "vomit", "vorarephilia", "voyeur", "vulgar", "vulva", "wang", "wank", "wazoo", "wedgie",
			"weed", "weenie", "weewee", "weiner", "wetback", "wetdream", "whitepower", "whiz", "wh0ar~", "wh0ars",
			"wh0ralicious", "wh0re^", "wh0ring^", "wigger", "windowlicker", "wiseass", "w0g~", "w00se", "w0p~", "wrappingmen",
			"wrinkledstarfish", "xrated", "xxx", "yaoi", "yeasty", "yellowshowers", "yiffy", "yobbo", "zibbi", "zoophilia",
			"zubb", "пенис", "пизда", "хуй", "блядь", "ебать", "сука", "гандон", "манда", "пидор", "пидорас", "еблан", "ебучий", "ебучая", "ебучее", "ебучие", "ебучий", "ебучая", "ебучее", "ебучие",
			"ебучий", "ебучая", "ебучее", "ебучие", "ебучий", "ебучая", "ебучее", "ебучие", "хуй", "хуя", "хуе", "хуи", "хуяра",  "хуяр", "хуяра", "хуярь", "хуйня", "хуйнище", "хуйня", "хуйнище", "хуйня", "хуйнище", "хуйня", "хуйнище", "хуйня", "хуйнище", "хуйня", "хуйнище", "хуйня","пиздец","уебан", "уебок", "уебки", "уебок", "уебан", "уебана", "уебано", "уебаны", "уебан", "уебана", "уебано", "уебаны","уеблан","сучка", "сука", "суки","пидорас", "пидор", "пидорас", "пидорка", "пидорки", "пидорок", "пидорос", "пидоросы", "пидорство", "пидорский", "пидорская", "пидорское", "пидорские","еблан","ебан","ебать","ебал","ебало","ебальник","ебанутый","ебаный","ебаная","ебаное","ебаные","дурак","дура","дурак","дураки","дебил","дебила","дебилы","идиот","идиота","идиоты","тупой","тупая","тупое","тупые", "писюн", "писька", "писюна", "писюн", "писька", "писюна", "писюн", "писька", "писюна", "писюн", "писька", "писюна", "писюн", "педофилия", "педофил", "педофила", "педофилы", "педофильский", "педофильская", "педофильское", "педофильские", "гандон", "гандоны", "гандонский", "гандонская", "гандонское", "гандонские", "сперма", "сперматозоид", "сперматозоида", "сперматозоиды", "сперматозоидный", "сперматозоидная", "сперматозоидное", "сперматозоидные", "гандон", "гандоны", "гандонский", "гандонская", "гандонское", "гандонские", "гей", "гей", "геи", "гейский", "гейская", "гейское", "гейские", "пидор", "пидорас", "пидорка", "пидорки", "пидорок", "пидорос", "пидоросы", "пидорство", "пидорский", "пидорская", "пидорское", "пидорские", "свастика", "свастики", "свастический", "свастическая", "свастическое", "свастические", "хуй", "хуя", "хуе", "хуи", "хуяра",  "хуяр", "хуяра", "гитлер", "гитлерский", "гитлерская", "гитлерское", "гитлерские", "пісюн","довбайоб", "довбойоб", "довбойобка", "довбойобки", "довбойобок", "довбойобос", "довбойобы", "довбойобский", "довбойобская", "довбойобское", "довбойобские", "шлюха", "шлюхи", "шлюшка", "шлюшки", "шлюхой", "шлюхой", "шлюхой", "classic us", "classicus","dlovan", "dlovanus", "dlovany", "dlovansk", "dlovanska", "dlovansko", "dlovans", "TGF1","tgf1","пиздаша", "пиздашка", "пиздюлина", "пиздюлка", "пиздюлька", "пиздюлина", "пиздюлка", "пиздюлька", "пиздюлина", "пиздюлка", "пиздюлька", "пиздюлина", "пиздюлка", "пиздюлька", "auad", "auadus", "auadum", "auadus", "auadus", "auadus", "auadus", "бандера", "бандеровец", "бандеровцы", "бандеровский", "бандеровская", "бандеровское", "бандеровские", "хохол", "хохлы", "хохолский", "хохольский", "хохольская", "хохольское", "хохольские", "зоофилия", "зоофил", "зоофила", "зоофилы", "зоофильский", "зоофильская", "зоофильское", "зоофильские", "трахать", "трахаться", "трахнул", "трахнуть", "трахнуться", "трахнутый", "трахнутые", "трахующий", "трахующая", "трахующее", "трахующие"
		};
		SkipList = new LetterTree();
		for (int i = 0; i < AllWords.Length; i++)
		{
			SkipList.AddWord(AllWords[i]);
		}
	}

	public static bool ContainsWord(string chatText)
	{
		for (int i = 0; i < chatText.Length; i++)
		{
			if (SkipList.Search(chatText, i) > 0)
			{
				return true;
			}
		}
		return false;
	}

	public static string CensorWords(string chatText)
	{
		StringBuilder stringBuilder = new StringBuilder(chatText);
		for (int i = 0; i < chatText.Length; i++)
		{
			int num = SkipList.Search(stringBuilder, i);
			if (num > 0)
			{
				CensorMatchedWord(stringBuilder, i, num);
				i = i + num - 1;
			}
		}
		return stringBuilder.ToString();
	}

	public static string UseTmpFontForCensorMarks(string chatText)
	{
		if (string.IsNullOrEmpty(chatText) || chatText.IndexOf(CensorChar) < 0)
		{
			return chatText;
		}
		StringBuilder stringBuilder = new StringBuilder(chatText.Length + 32);
		for (int i = 0; i < chatText.Length; i++)
		{
			if (chatText[i] != CensorChar)
			{
				stringBuilder.Append(chatText[i]);
				continue;
			}
			stringBuilder.Append("<font=\"");
			stringBuilder.Append(CensorTmpFontName);
			stringBuilder.Append("\">");
			while (i < chatText.Length && chatText[i] == CensorChar)
			{
				stringBuilder.Append(CensorChar);
				i++;
			}
			stringBuilder.Append("</font>");
			i--;
		}
		return stringBuilder.ToString();
	}

	private static void CensorMatchedWord(StringBuilder text, int start, int length)
	{
		int end = start + length;
		for (int i = start; i < end; i++)
		{
			if (!SkipList.IsFiller(text[i]))
			{
				text[i] = CensorChar;
			}
		}
	}

	private static bool IsLetter(char letter)
	{
		switch (letter)
		{
		case ' ':
			return false;
		case '\r':
			return false;
		case '\n':
			return false;
		default:
			if (SymbolChars.Contains(letter))
			{
				return false;
			}
			return true;
		}
	}
}
