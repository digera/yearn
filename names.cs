﻿using System;
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
        "Bobby","Billy","Ricky","Tommy","Johnny","Jimmy","Timmy","Danny","Manny","Lenny","Kenny","Benny","Jenny","Penny","Denny","Henry","Harry","Samuel","Daniel","David","Joseph","Joshua","Jacob","James","John","Michael","Matthew","Andrew","Anthony","Nicholas","Christopher","Ryan","Justin","Brandon","William","Jonathan","Austin","Kevin","Robert","Thomas","Zachary","Alexander","Cody","Jordan","Kyle","Benjamin","Aaron","Richard","Tyler","Steven","Charles","Patrick","Jeremy","Brian","Eric","Stephen","Adam","Joseph","Dylan","Nathan","Sean","Cameron","Ethan","Christian","Samuel","Jason","Isaac","Caleb","Logan",
        "Schlomo","Yitzhak","Yosef","Yehuda","Moshe","David","Avraham","Shmuel","Yitzchak","Yakov","Yosef","Yehoshua","Yehuda","Yonatan","Yair","Yisrael","Yehonatan","Yehoyada","Yehoyachin","Yehoram","Yehoshafat","Yehoyakim","Yehoyariv","Yehoyada","Yehoyakin",
        "Leroy","Lamar","Lionel","Luther","Lloyd","Lyle"




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

public enum StoneType
{
    Earth,          // The baseline: raw, unrefined dirt
    Clay,           // Slightly more structured than Earth
    Pebble,         // Small, basic rock fragments
    Siltstone,      // Fine-grained sedimentary start
    Sandstone,      // Common, gritty, and porous
    Shale,          // Layered sedimentary rock
    Limestone,      // Soft, calcium-rich sedimentary
    Mudstone,       // Compact but unremarkable
    Rock,           // Generic, everyday stone
    Hardstone,      // A step up in durability
    Basalt,         // Dark, volcanic, and tough
    Slate,          // Thin, workable, metamorphic
    Granite,        // Coarse, strong, igneous classic
    Marble,         // Polished, metamorphic beauty
    Quartzite,      // Hardened quartz-rich rock
    Gneiss,         // Banded, high-pressure metamorphic
    Schist,         // Shiny, flaky, and layered
    Porphyry,       // Speckled igneous stone
    Obsidian,       // Volcanic glass, sharp and sleek
    Flint,          // Hard, spark-making sedimentary
    Chert,          // Similar to flint, but finer
    Jade,           // Tough, ornamental green stone
    Serpentine,     // Smooth, greenish, and twisty
    Travertine,     // Light, porous, and elegant
    Onyx,           // Sleek, banded, and dark
    Lapis,          // Deep blue semi-precious stone
    Malachite,      // Vibrant green, swirly patterns
    Turquoise,      // Blue-green desert gem
    Agate,          // Colorful, banded chalcedony
    Jasper,         // Opaque, earthy gemstone
    Amethyst,       // Purple quartz beauty
    Citrine,        // Golden-yellow quartz
    Topaz,          // Durable, colorful gem
    Garnet,         // Deep red and resilient
    Aquamarine,     // Pale blue beryl gem
    Sapphire,       // Brilliant blue corundum
    Ruby,           // Fiery red corundum
    Emerald,        // Lush green beryl
    Opal,           // Iridescent, play-of-color gem
    Moonstone,      // Ethereal, glowing feldspar
    Sunstone,       // Warm, sparkling feldspar
    Starstone,      // Fictional: shimmering with star-like flecks
    Voidrock,       // Fictional: dark, energy-absorbing stone
    Luminite,       // Fictional: faintly glowing rare mineral
    Diamond,        // The pinnacle: hardest natural gem
    Aetherstone     // Fictional: mystical, ultimate upgrade
}


