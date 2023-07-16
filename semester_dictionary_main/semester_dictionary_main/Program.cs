using System;
using System.DirectoryServices;
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

    public class ItemAlreadyExistsException : InvalidOperationException
    {
        public ItemAlreadyExistsException() : base() { }
        public ItemAlreadyExistsException(string message) : base(message) { }
        public ItemAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
    }

    public class ItemNotFoundException : InvalidOperationException
    {
        public ItemNotFoundException() : base() { }
        public ItemNotFoundException(string message) : base(message) { }
        public ItemNotFoundException(string message, Exception inner) : base(message, inner) { }
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

        #region PUBLIC METHODS
        public void Remove()
        {
            DeleteWordForms();

            central.RemoveWord(this);
        }

        public void ChangeForm(string newBase)
        {
            baseForm = newBase;
            foreach(WordForm form in forms)
            {
                form.ChangeForm(newBase);
            }
        }

        public void ChangePronunciation(string newBase)
        {
            basePron = newBase;
            foreach (WordForm form in forms)
            {
                form.ChangePronunciation(newBase);
            }
        }

        public void ChangeRhyme(string newBase)
        {
            baseRhyme = newBase;
            foreach (WordForm form in forms)
            {
                form.ChangeRhyme(newBase);
            }
        }

        public void changeTranslation(string newTrans)
        {
            translation = newTrans;
        }

        public void changeDefinition(string newDef)
        {
            definition = newDef;
        }

        public void changeClass(WordClass newClass)
        {
            partOfSpeech = newClass.GetPoS();
            wClass = newClass;
            DeleteWordForms();
            CreateWordForms();
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

            foreach (Declension declension in wClass.GetDeclensions())
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
            form = DeriveForm(parent.getForm(), declension.getFormTransform());
            pronunciation = DeriveForm(parent.getPronunciation(), declension.getPronTransform());
            this.rhyme = EvaluateRhyme(DeriveForm(parent.getRhyme(), declension.getRhymeTransform()));
            // assigns the wordform a rhyme group based on the transformed rhyme pattern of the parent

            central.AddWordForm(this);
        }
        #endregion

        #region PUBLIC METHODS
        public void Remove()
        {
            central.RemoveWordForm(this);
            rhyme.RemoveForm(this);
        }

        public void ChangeForm(string newBase)
        {
            form = DeriveForm(newBase, declension.getFormTransform());
        }

        public void ChangePronunciation(string newBase)
        {
            pronunciation = DeriveForm(newBase, declension.getPronTransform());
        }

        public void ChangeRhyme(string newBase)
        {
            RemoveSelfFromRhymeGroup();
            rhyme = EvaluateRhyme(newBase);
        }
        #endregion

        #region PRIVATE METHODS
        private RhymeGroup EvaluateRhyme(string rhymeLiteral)
        {
            return central.AssignRhymeGroup(rhymeLiteral, this);
        }

        private void RemoveSelfFromRhymeGroup()
        {
            rhyme.RemoveForm(this);
            rhyme = null;
        }

        private string DeriveForm(string baseForm, List<TransformUnit> rules)
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
        public string getName()
        {
            return name;
        }

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

        #region PUBLIC METHODS
        public void Rename(string newName)
        {
            name = newName;
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
        public List<Declension> GetDeclensions()
        {
            return declensions;
        }

        public PoS GetPoS()
        {
            return parent;
        }
        #endregion

        #region PUBLIC METHODS
        public void AddDeclension(string dName)
        {
            /* 
             * may only be called by the parent PoS.
             */
            declensions.Add(new Declension(dName, this, central));
        }

        public void RemoveDeclension(string dName)
        {
            /*
             * may only be called by the parent PoS.
             */
            foreach(Declension dec in declensions)
            {
                if (dec.getName() == dName)
                {
                    declensions.Remove(dec);
                    break;
                }
            }
        }

        public void RenameDeclension(string oldName, string newName)
        {
            /*
             * May only be called by the parent PoS
             */
            foreach (Declension dec in declensions)
            {
                if (dec.getName() == oldName)
                {
                    dec.Rename(newName);
                    break;
                }
            }
        }

        public void Rename(string newName)
        {
            this.name = newName;
        }
        #endregion
    }

    public class PoS
    {
        /*
         * Part of speech, such as a noun or a verb.
         * Manages its word classes, and its declensions.
         * Declensions are handled by each word class separately,
         * but they may only be added, removed, or renamed in all 
         * word classes at the same time, through the parent PoS.
         * Declension effects are then handled individually for each
         * declension object.
         */
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
            AddWordClass(name); // each PoS needs at least one WordClass to operate.
        }
        #endregion

        #region PUBLIC METHODS
        public void AddWordToSelf(Word word)
        {
            this.words.Add(word);
        }

        public void AddWordClass(string name)
        {
            WordClass wClass = new WordClass(name, this, central);
            foreach (string declension in declensions)
            {
                wClass.AddDeclension(declension);
            }
        }

        public void RemoveWordClass(WordClass wClass)
        {
            if (wordClasses.Count == 1)
            {
                throw new InvalidOperationException("Trying to remove the only wordClass of a PoS. A PoS needs at least one word class to operate.")
            }
            wordClasses.Remove(wClass);
        }

        public void AddDeclension(string name)
        {
            if (declensions.Contains(name))
            {
                throw new ItemAlreadyExistsException(string.Format("Tried to create a declension with the name {0}, but it already exists.", name));
            }
            declensions.Add(name);
            foreach (WordClass wClass in wordClasses)
            {
                wClass.AddDeclension(name);
            }
        }

        public void RemoveDeclension(string name)
        {
            if (!declensions.Contains(name))
            {
                throw new ItemNotFoundException(string.Format("Declension {0} was not found in this PoS.", name));
            }
            declensions.Remove(name);
            foreach(WordClass wClass in wordClasses)
            {
                wClass.RemoveDeclension(name);
            }
        }

        public void RenameDeclension(string oldName, string newName)
        {
            if (!declensions.Contains(oldName))
            {
                throw new ItemNotFoundException(string.Format("Declension {0} was not found in this PoS.", oldName));
            }
            if (declensions.Contains(newName))
            {
                throw new ItemAlreadyExistsException(string.Format("Tried to rename a declension to {0}, but one with that name already exists.", newName));
            }

            declensions.Remove(oldName);
            declensions.Add(newName);

            foreach (WordClass wClass in wordClasses)
            {
                wClass.RenameDeclension(oldName, newName);
            }
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
            if (rhymes.Count == 0) central.RemoveRhymeGroup(this);
        }
        #endregion

        #region PRIVATE METHODS

        #endregion
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
                found = CreateRhymeGroup(rhymeLiteral, from);
            }

            return found;
        }
        public void RemoveRhymeGroup(RhymeGroup group)
        {
            RhymeGroupList.Remove(group);
        }

        public void AddWordForm(WordForm form)
        {
            formList.Add(form);
        }

        public void RemoveWordForm(WordForm form)
        {
            formList.Remove(form);
        }

        public void AddWord(Word word)
        {
            wordList.Add(word);
            transList.Add(word);
        }

        public void RemoveWord(Word word)
        {
            wordList.Remove(word);
            transList.Remove(word);
        }
        #endregion

        #region PRIVATE METHODS
        private RhymeGroup CreateRhymeGroup(string id, WordForm firstWord)
        {
            RhymeGroup newGroup = new RhymeGroup(id, this);
            newGroup.Insert(firstWord);
            RhymeGroupList.Add(newGroup);
            return newGroup;
        }
        #endregion
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