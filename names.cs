using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct Names
{
    public static readonly string[] CommonNames = new string[]
    {
        "James", "Cody", "Chris", "Robert", "Drew", "Josh", "Tristan","Miguel",
        "Michael", "John", "David", "William", "Joseph", "Thomas", "Charles",
        "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul",
        "Andrew", "Joshua", "Kenneth", "Kevin", "Brian", "George", "Edward",
        "Ronald", "Timothy", "Jason", "Jeffrey", "Ryan", "Jacob", "Gary",
        "Nicholas", "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott",
        "Brandon", "Benjamin", "Samuel", "Gregory", "Frank", "Alexander",
        "Raymond", "Patrick", "Jack", "Dennis", "Jerry", "Tyler", "Aaron",
        "Jose", "Adam", "Henry", "Nathan", "Douglas", "Peter",
        "Kyle", "Walter", "Ethan", "Jeremy", "Harold", "Keith", "Christian",
        "Roger", "Noah", "Gerald", "Carl", "Terry", "Sean", "Austin", "Arthur",
        "Lawrence", "Jesse", "Dylan", "Bryan", "Joe", "Jordan", "Billy",
        "Bruce", "Albert", "Willie", "Gabriel", "Logan", "Alan", "Juan",
        "Wayne", "Roy", "Ralph", "Randy", "Eugene", "Vincent", "Russell",
        "Elijah", "Louis", "Bobby", "Philip", "Johnny","Howard", "Martin","Victor","Frederick","Eizo","Liam","Mason","Ethan","Lucas","Oliver","Aiden","Elijah","James","Benjamin","William","Michael","Alexander","Daniel","Matthew","Henry","Joseph","Jackson","Samuel","Sebastian","David","Carter","Wyatt","Jayden","John","Owen","Dylan","Luke","Gabriel","Anthony","Isaac","Grayson","Jack","Julian","Levi","Christopher","Joshua","Andrew","Lincoln","Mateo","Ryan","Jaxon","Nathan","Aaron","Isaiah","Thomas","Charles","Caleb","Josiah",
        "Alpha","Bravo","Charlie","Delta","Echo","Foxtrot","Golf","Hotel","India","Juliet","Kilo","Lima","Mike","November","Oscar","Papa","Quebec","Romeo","Sierra","Tango","Uniform","Victor","Whiskey","Xray","Yankee","Zulu",
        "Monkey","Lion","Tiger","Bear","Wolf","Fox","Dog","Cat","Elephant","Giraffe","Horse","Cow","Pig","Sheep","Goat","Chicken","Duck","Goose","Turkey","Penguin","Ostrich","Parrot","Eagle","Hawk","Falcon","Owl","Raven","Crow","Sparrow","Robin","Bluejay","Cardinal","Finch","Canary","Goldfinch","Bullfin"
        ,"Warbler","Wren","Thrush","Swallow","Swift","Hummingbird","Kingfisher","Woodpecker","Nuthatch","Chickadee","Titmouse","Starling","Blackbird","Grackle","Oriole","Tanager","Bunting","Grosbeak","Sparrow","Junco","Snowbird","Bunting",
        "Jaden","Brayden","Caden","Aiden","Kaden","Hayden","Jayden","Zayden","Raiden","Jaxen","Maxen","Paxen","Daxen","Axel","Max","Jax","Dax","Pax","Rex","Tex","Lex","Hex","Vex","Nex","Zex","Xex","Yex","Wex","Qex","Kex","Jex","Bex","Cex","Dex","Fex","Gex",
        



    };
    private static HashSet<string> usedNames = new HashSet<string>();

    public static string GetUniqueName()
    {
        // If all names are used, clear the used names (optional)
        if (usedNames.Count >= CommonNames.Length)
        {
            usedNames.Clear();
        }

        // Get available names
        var availableNames = CommonNames.Where(name => !usedNames.Contains(name)).ToList();

        // Pick a random available name
        Random random = new Random();
        string selectedName = availableNames[random.Next(availableNames.Count)];

        // Mark it as used
        usedNames.Add(selectedName);

        return selectedName;
    }

    // When loading a saved game, register existing names
    public static void RegisterName(string name)
    {
        if (CommonNames.Contains(name))
        {
            usedNames.Add(name);
        }
    }

    // Optional: Clear all used names if needed
    public static void ClearUsedNames()
    {
        usedNames.Clear();
    }

}



