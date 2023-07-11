namespace semester_dictionary_main
{

    public class Word
    {
        private string baseForm = "";
        private string baseRhyme = "";
        private Word? counterpart;
        private PoS partOfSpeech;
        private WordClass wClass;


        public Word(string form, string rhyme, PoS partOfSpeech, WordClass wClass)
        {
            baseForm = form;
            baseRhyme = rhyme;
            this.partOfSpeech = partOfSpeech;
            this.wClass = wClass;
        }

        public void setCounterpart(Word other)
        {
            counterpart = other;
        }
    }

    public class WordForm
    {
        private Word parent;
        private string form;
        private RhymeGroup rhyme;
        private Declension declension;

        public WordForm(Word baseWord, Declension rule)
        {
            parent = baseWord;
            declension = rule;
            form = ""; // create form by transforming base form with rule's regex
            // determine rhyme group form, then search all rhyme groups, if not found, create new.
        }
    }

    public class Declension
    {
        private string name;
        private string formRegex = "";
        private string rhymeRegex = "";
        private WordClass parent;

        public Declension(string name, WordClass parent)
        {
            this.name = name;
            this.parent = parent;
        }
    }

    public class WordClass
    {
        private string name;
        private PoS parent;
        private List<Declension> declensions = new List<Declension>();

        public WordClass(string name, PoS parent)
        {
            this.name = name;
            this.parent = parent;
        }
    }

    public class PoS
    {
        private string name;
        private List<WordClass> wordClasses = new List<WordClass>();
        private List<Word> words = new List<Word>();
        private List<string> declensions = new List<string>();

        public PoS(string name)
        {
            this.name = name;
        }
    }

    public class RhymeGroup
    {
        private string id;
        private List<WordForm> rhymes = new List<WordForm>();

        public RhymeGroup(string id)
        {
            this.id = id;
        }
    }

    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}