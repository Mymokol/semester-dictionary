using System;
using System.Text.RegularExpressions;

namespace semester_dictionary_main
{


    #region EXCEPTIONS
    public class ListNotEmptyException : Exception
    {
        public ListNotEmptyException() : base() { }
        public ListNotEmptyException(string message) : base(message) { }
        public ListNotEmptyException(string message, Exception inner) : base(message, inner) { }
    }
    #endregion


    #region STRUCTURES
    public struct TransformUnit
    {
        public string identifier = "";
        public string regex = "";
        public string replace = "";

        public TransformUnit(string identifier, string regex, string replace)
        {
            this.identifier = identifier;
            this.regex = regex;
            this.replace = replace;
        }
    }
    #endregion

    public class Word
    {
        #region ATTRIBUTES AND CONSTRUCTOR
        private CentralStorage central;
        private string baseForm = "";
        private string basePron = "";
        private string baseRhyme = "";
        private string translation = "";
        private string definition = "";
        private PoS partOfSpeech;
        private WordClass wClass;
        private List<WordForm> forms = new List<WordForm>();


        public Word(string form, string pronunciation, string rhyme, string translation, 
        string definition, PoS partOfSpeech, WordClass wClass, CentralStorage central)
        {
            baseForm = form;
            basePron = pronunciation;
            baseRhyme = rhyme;
            this.partOfSpeech = partOfSpeech;
            this.wClass = wClass;
            this.translation = translation;
            this.definition = definition;
            this.central = central;

            CreateWordForms();
        }
        #endregion

        #region GETTERS
        public string getForm()
        {
            return baseForm;
        }

        public string getPronunciation()
        {
            return basePron;
        }

        public string getRhyme()
        {
            return baseRhyme;
        }

        public string getTranslation()
        {
            return translation;
        }

        public string getDefinition()
        {
            return definition;
        }

        public PoS getPartOfSpeech()
        {
            return partOfSpeech;
        }

        public WordClass getWordClass()
        {
            return wClass;
        }
        #endregion

        #region PRIVATE METHODS
        private void CreateWordForms()
        {
            /*
             * Creates WordForms and stores them in its 'forms' list.
             * The list must be empty before new wordforms are created, 
             * otherwise throws a ListNotEmptyException.
             */

            if (forms.Count != 0)
            {
                throw new ListNotEmptyException("Tried to create WordForms while some WordForms already existed");
            }

            foreach (Declension declension in wClass.getDeclensions())
            {
                forms.Add(new WordForm(this, declension, central));
            }
        }

        private void DeleteWordForms()
        {
            foreach (WordForm form in forms)
            {
                form.Remove();
                forms.Remove(form);
            }
        }
        #endregion
    }

    public class WordForm
    {
        #region ATTRIBUTES AND CONSTRUCTOR
        private CentralStorage central;
        private Word parent;
        private string form;
        private string pronunciation;
        private RhymeGroup rhyme;
        private Declension declension;

        public WordForm(Word baseWord, Declension rule, CentralStorage central)
        {
            this.central = central;
            parent = baseWord;
            declension = rule;
            form = deriveForm(parent.getForm(), declension.getFormTransform());
            pronunciation = deriveForm(parent.getPronunciation(), declension.getPronTransform());
            this.rhyme = evaluateRhyme(deriveForm(parent.getRhyme(), declension.getRhymeTransform()));
            // assigns the wordform a rhyme group based on the transformed rhyme pattern of the parent

            central.addWordForm(this);
        }
        #endregion

        #region PUBLIC METHODS
        public void Remove()
        {
            central.removeWordForm(this);
            rhyme.RemoveForm(this);
        }
        #endregion

        #region PRIVATE METHODS
        private RhymeGroup evaluateRhyme(string rhymeLiteral)
        {
            return central.AssignRhymeGroup(rhymeLiteral, this);
        }

        private string deriveForm(string baseForm, List<TransformUnit> rules)
        {
            // returns the base form altered by all the transform pairs in the declension.

            string transForm = baseForm; // iteratively changed in the cycle

            foreach (TransformUnit unit in rules)
            {
                if (Regex.IsMatch(transForm, unit.identifier))
                {
                    transForm = Regex.Replace(transForm, unit.regex, unit.replace);
                }
            }

            return transForm;
        }
        #endregion
    }

    public class Declension
    {
        #region ATTRIBUTES AND CONSTRUCTOR
        private CentralStorage central;
        private string name;
        private List<TransformUnit> formTransform = new List<TransformUnit>();
        private List<TransformUnit> pronTransform = new List<TransformUnit>();
        private List<TransformUnit> rhymeTransform = new List<TransformUnit>();
        private WordClass parent;

        public Declension(string name, WordClass parent, CentralStorage central)
        {
            this.name = name;
            this.parent = parent;
            this.central = central;
        }
        #endregion

        #region GETTERS
        public List<TransformUnit> getFormTransform()
        {
            return formTransform;
        }

        public List<TransformUnit> getPronTransform()
        {
            return pronTransform;
        }

        public List<TransformUnit> getRhymeTransform()
        {
            return rhymeTransform;
        }
        #endregion
    }

    public class WordClass
    {
        #region ATTRIBUTES AND CONSTRUCTOR
        private CentralStorage central;
        private string name;
        private PoS parent;
        private List<Declension> declensions = new List<Declension>();

        public WordClass(string name, PoS parent, CentralStorage central)
        {
            this.name = name;
            this.parent = parent;
            this.central = central;
        }
        #endregion

        #region GETTERS
        public List<Declension> getDeclensions()
        {
            return declensions;
        }
        #endregion
    }

    public class PoS
    {
        #region ATTRIBUTES AND CONSTRUCTOR
        private CentralStorage central;
        private string name;
        private List<WordClass> wordClasses = new List<WordClass>();
        private List<Word> words = new List<Word>();
        private List<string> declensions = new List<string>();

        public PoS(string name, CentralStorage central)
        {
            this.name = name;
            this.central = central;
        }
        #endregion
    }

    public class RhymeGroup
    {
        #region ATTRIBUTES AND CONSTRUCTOR
        private CentralStorage central;
        private string id; 
        // string that identifies the group in the user's preferred rhyming notation.
        private List<WordForm> rhymes = new List<WordForm>();
        // list of all word forms that fall into this rhyme group (their rhyme matches the group's id).

        public RhymeGroup(string id, CentralStorage central)
        {
            this.id = id;
            this.central = central;
        }
        #endregion

        #region GETTERS
        public string GetID()
        {
            return this.id;
        }
        #endregion

        #region PUBLIC METHODS
        public void Insert(WordForm form)
        {
            rhymes.Add(form);
        }

        public void RemoveForm(WordForm form)
        {
            rhymes.Remove(form);
        }
    }


    public class CentralStorage
    {
        #region ATTRIBUTES AND CONSTRUCTOR
        private List<Word> wordList = new List<Word>();
        private List<Word> transList = new List<Word>();
        private List<WordForm> formList = new List<WordForm>();
        private List<PoS> PoSList = new List<PoS>();
        private List<RhymeGroup> RhymeGroupList = new List<RhymeGroup>();
        #endregion

        #region PUBLIC METHODS
        public RhymeGroup AssignRhymeGroup(string rhymeLiteral, WordForm from)
        {
            RhymeGroup? found = null;

            foreach (RhymeGroup group in RhymeGroupList)
            {
                if (group.GetID() == rhymeLiteral)
                {
                    found = group;
                    break;
                }
            }

            if (found == null)
            {
                found = createRhymeGroup(rhymeLiteral, from);
            }

            return found;
        }

        public void addWordForm(WordForm form)
        {
            formList.Add(form);
        }

        public void removeWordForm(WordForm form)
        {
            formList.Remove(form);
        }

        public void addWord(Word word)
        {
            wordList.Add(word);
            transList.Add(word);
        }

        public void removeWord(Word word)
        {
            wordList.Remove(word);
            transList.Remove(word);
        }
        #endregion

        #region PRIVATE METHODS
        private RhymeGroup createRhymeGroup(string id, WordForm firstWord)
        {
            RhymeGroup newGroup = new RhymeGroup(id, this);
            newGroup.Insert(firstWord);
            RhymeGroupList.Add(newGroup);
            return newGroup;
        }

        private void removeRhymeGroup(RhymeGroup group)
        {
            RhymeGroupList.Remove(group);
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