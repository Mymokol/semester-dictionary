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
        string definition, WordClass wClass, CentralStorage central)
        {
            baseForm = form;
            basePron = pronunciation;
            baseRhyme = rhyme;
            this.partOfSpeech = wClass.GetPoS();
            this.wClass = wClass;
            this.translation = translation;
            this.definition = definition;
            this.central = central;

            CreateWordForms();
        }
        #endregion

        #region GETTERS
        public string GetForm()
        {
            return baseForm;
        }

        public string GetPronunciation()
        {
            return basePron;
        }

        public string GetRhyme()
        {
            return baseRhyme;
        }

        public string GetTranslation()
        {
            return translation;
        }

        public string GetDefinition()
        {
            return definition;
        }

        public PoS GetPartOfSpeech()
        {
            return partOfSpeech;
        }

        public WordClass GetWordClass()
        {
            return wClass;
        }

        public List<WordForm> GetWordForms()
        {
            return forms;
        }

        public WordForm GetWordForm(string name)
            /*
             * returns a wordform with the given form, or null if
             * such a form doesn't exist
             */
        {
            foreach (WordForm form in forms) // ALPHABETICAL MODIFY
            {
                if (form.GetForm() == name)
                {
                    return form;
                }
            }
            return null;
        }
        #endregion

        #region PUBLIC METHODS
        public void UpdateDeclension(Declension declension)
        {
            foreach (WordForm form in forms)
            {
                if (form.GetDeclension() == declension)
                {
                    form.Update();
                    break;
                }
            }
        }

        public void Remove()
        {
            /*
             * Propagates removal to its children WordForms.
             * May only be called from the central.
             */
            DeleteWordForms();
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

        public void ChangeTranslation(string newTrans)
        {
            translation = newTrans;
        }

        public void ChangeDefinition(string newDef)
        {
            definition = newDef;
        }

        public void ChangeClass(WordClass newClass)
        {
            wClass.RemoveWordFromSelf(this);
            partOfSpeech.RemoveWordFromSelf(this);
            partOfSpeech = newClass.GetPoS();
            partOfSpeech.AddWordToSelf(this);
            wClass = newClass;
            wClass.AddWordToSelf(this);
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
            form = DeriveForm(parent.GetForm(), declension.GetFormTransform());
            pronunciation = DeriveForm(parent.GetPronunciation(), declension.GetPronunciationTransform());
            this.rhyme = EvaluateRhyme(DeriveForm(parent.GetRhyme(), declension.GetRhymeTransform()));
            // assigns the wordform a rhyme group based on the transformed rhyme pattern of the parent

            central.AddWordForm(this);
        }
        #endregion

        #region GETTERS
        public Word GetParent()
        {
            return parent;
        }
        public string GetForm()
        {
            return form;
        }
        public string GetPronunciation()
        {
            return pronunciation;
        }
        public RhymeGroup GetRhymeGroup()
        {
            return rhyme;
        }
        public Declension GetDeclension()
        {
            return declension;
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
            form = DeriveForm(newBase, declension.GetFormTransform());
        }

        public void ChangePronunciation(string newBase)
        {
            pronunciation = DeriveForm(newBase, declension.GetPronunciationTransform());
        }

        public void ChangeRhyme(string newBase)
        {
            RemoveSelfFromRhymeGroup();
            rhyme = EvaluateRhyme(newBase);
        }

        public void Update()
        {
            ChangeForm(parent.GetForm());
            ChangePronunciation(parent.GetPronunciation());
            ChangeRhyme(parent.GetRhyme());
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
        public string GetName()
        {
            return name;
        }

        public List<TransformUnit> GetFormTransform()
        {
            return formTransform;
        }

        public List<TransformUnit> GetPronunciationTransform()
        {
            return pronTransform;
        }

        public List<TransformUnit> GetRhymeTransform()
        {
            return rhymeTransform;
        }

        public WordClass GetWordClass()
        {
            return parent;
        }

        public PoS GetPoS()
        {
            return parent.GetPoS();
        }
        #endregion

        #region PUBLIC METHODS
        public void Rename(string newName)
        {
            name = newName;
        }

        public void AddFormTrans(TransformUnit transform)
        {
            AddTransform(transform, formTransform);
        }
        public void AddFormTrans(string identifier, string regex, string replace)
        {
            AddTransform(new TransformUnit(identifier, regex, replace), formTransform);
        }

        public void AddPronTrans(TransformUnit transform)
        {
            AddTransform(transform, pronTransform);
        }
        public void AddPronTrans(string identifier, string regex, string replace)
        {
            AddTransform(new TransformUnit(identifier, regex, replace), pronTransform);
        }

        public void AddRhymeTrans(TransformUnit transform)
        {
            AddTransform(transform, rhymeTransform);
        }
        public void AddRhymeTrans(string identifier, string regex, string replace)
        {
            AddTransform(new TransformUnit(identifier, regex, replace), rhymeTransform);
        }



        #endregion

        #region PRIVATE METHODS
        private void AddTransform(TransformUnit transform, List<TransformUnit> where)
        {
            where.Add(transform);
            LetWordsKnowOfChange();
        }
        private void RemoveTransform(TransformUnit transform, List<TransformUnit> from)
        {
            from.Remove(transform);
            LetWordsKnowOfChange();
        }
        private void LetWordsKnowOfChange()
        {
            parent.NotifyOfDeclensionChange(this);
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
        private List<Word> words = new List<Word>();

        public WordClass(string name, PoS parent, CentralStorage central)
        {
            this.name = name;
            this.parent = parent;
            this.central = central;
        }
        #endregion

        #region GETTERS
        public string GetName()
        {
            return name;
        }
        public List<Declension> GetDeclensions()
        {
            return declensions;
        }
        
        public Declension GetDeclension(string name)
            /*
             * returns a declension object with the given name, or
             * null if such declension doesn't exist.
             */
        {
            foreach (Declension dec in declensions) // ALPHABETICAL MODIFY
            {
                if (dec.GetName() == name)
                {
                    return dec;
                }
            }
            return null;
        }

        public PoS GetPoS()
        {
            return parent;
        }

        public List<Word> GetWords()
        {
            return words;
        }
        #endregion

        #region PUBLIC METHODS
        public void NotifyOfDeclensionChange(Declension declension)
        {
            foreach (Word word in words)
            {
                word.UpdateDeclension(declension);
            }
        }

        public void AddWordToSelf(Word word)
        {
            words.Add(word);
        }
        
        public void RemoveWordFromSelf(Word word)
        {
            words.Remove(word);
        }

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
            foreach(Declension dec in declensions) // ALPHABETICAL MODIFY
            {
                if (dec.GetName() == dName)
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
            foreach (Declension dec in declensions) // ALPHABETICAL MODIFY
            {
                if (dec.GetName() == oldName)
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

        #region GETTERS
        public string GetName()
        {
            return name;
        }
        public List<WordClass> GetWordClasses()
        {
            return wordClasses;
        }
        public WordClass GetWordClass(string name)
        {
            foreach (WordClass wc in wordClasses) // ALPHABETICAL MODIFY
            /*
             * returns a wordclass object of the given name, or null
             * if such wordclass doesn't exist.
             */
            {
                if (wc.GetName() == name)
                {
                    return wc;
                }
            }
            return null;
        }
        public List<Word> GetWords()
        {
            return words;
        }

        public List<string> GetDeclensions()
        {
            return declensions;
        }
        #endregion

        #region PUBLIC METHODS
        public void AddWordToSelf(Word word)
        {
            this.words.Add(word);
        }
        public void RemoveWordFromSelf(Word word)
        {
            this.words.Remove(word);
        }

        public WordClass AddWordClass(string name)
        {
            WordClass wClass = new WordClass(name, this, central);
            foreach (string declension in declensions)
            {
                wClass.AddDeclension(declension);
            }
            wordClasses.Add(wClass);
            return wClass;
        }

        public void RemoveWordClass(WordClass wClass)
        {
            if (wordClasses.Count == 1)
            {
                throw new InvalidOperationException(
                    "Trying to remove the only wordClass of a PoS. A PoS needs at least one word class to operate.");
            }
            wordClasses.Remove(wClass);
        }

        public void AddDeclension(string name)
        {
            if (declensions.Contains(name))
            {
                throw new ItemAlreadyExistsException(string.Format(
                    "Tried to create a declension with the name {0}, but it already exists.",
                    name));
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
                throw new ItemNotFoundException(string.Format(
                    "Declension {0} was not found in this PoS.", name));
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
                throw new ItemNotFoundException(string.Format(
                    "Declension {0} was not found in this PoS.", oldName));
            }
            if (declensions.Contains(newName))
            {
                throw new ItemAlreadyExistsException(string.Format(
                    "Tried to rename a declension to {0}, but one with that name already exists.",
                    newName));
            }

            declensions.Remove(oldName);
            declensions.Add(newName);

            foreach (WordClass wClass in wordClasses)
            {
                wClass.RenameDeclension(oldName, newName);
            }
        }

        public void Rename(string newName)
        {
            name = newName;
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
        // ^^^ will differ from wordList once alphabetical indexing gets implemented
        private List<WordForm> formList = new List<WordForm>(); 
        // ^^^ MAY NOT BE NEEDED: Consider, eventually delete.
        private List<PoS> PoSList = new List<PoS>();
        private List<RhymeGroup> RhymeGroupList = new List<RhymeGroup>();
        #endregion

        #region GETTERS
        public List<Word> GetWordList()
        {
            return wordList;
        }
        public List<Word> GetTransList()
        {
            return transList;
        }
        public List<WordForm> GetFormList()
        {
            return formList;
        }
        public List<PoS> GetPoSList()
        {
            return PoSList;
        }
        public PoS GetPoS(string name)
            /*
             * returns a PoS object of the given name, or null if 
             * such PoS doesn't exist.
             */
        {
            foreach (PoS pos in PoSList) // ALPHABETICAL MODIFY
            {
                if (pos.GetName() == name)
                {
                    return pos;
                }
            }
            return null;
        }
        public List<RhymeGroup> GetRhymeGroupList()
        {
            return RhymeGroupList;
        }

        #endregion

        #region PUBLIC METHODS
        public RhymeGroup AssignRhymeGroup(string rhymeLiteral, WordForm from)
        {
            RhymeGroup? found = null;

            foreach (RhymeGroup group in RhymeGroupList) // ALPHABETICAL MODIFY
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
            /*
             * May only be called from Word objects.
             * MAY NOT BE NEEDED: Consider, eventually delete.
             */
            formList.Add(form);
        }

        public void RemoveWordForm(WordForm form)
        {
            /*
             * May only be called from Word objects.
             * MAY NOT BE NEEDED: Consider, eventually delete.
             */
            formList.Remove(form);
        }

        public Word AddWord(Word word)
        {
            wordList.Add(word);
            transList.Add(word);
            word.GetPartOfSpeech().AddWordToSelf(word);
            word.GetWordClass().AddWordToSelf(word);
            return word;
        }

        public Word AddWord(string form, string pronunciation, string rhyme,
            string translation, string definition, WordClass wClass)
        {
            return AddWord(new Word(form, pronunciation, rhyme, translation, definition,
                wClass, this));
        }

        public void RemoveWord(Word word)
        {
            wordList.Remove(word);
            transList.Remove(word);
            word.GetPartOfSpeech().RemoveWordFromSelf(word);
            word.GetWordClass().RemoveWordFromSelf(word);
        }

        public void EditWord(Word word, string newForm, string newPron, string newRhyme,
            string newTrans, string newDef, WordClass newClass)
        {
            word.ChangeForm(newForm);
            word.ChangePronunciation(newPron);
            word.ChangeRhyme(newRhyme);
            word.ChangeTranslation(newTrans);
            word.ChangeDefinition(newDef);
            word.ChangeClass(newClass);
        }

        public PoS AddPoS(PoS partOfSpeech)
        {
            PoSList.Add(partOfSpeech);
            return partOfSpeech;
        }

        public PoS AddPoS(string name)
        {
            return AddPoS(new PoS(name, this));
        }

        public void RemovePoS(PoS partOfSpeech)
        {
            PoSList.Remove(partOfSpeech);
        }

        public void RenamePoS(PoS partOfSpeech, string newName)
        {
            partOfSpeech.Rename(newName);
        }

        public WordClass CreateWordClass(PoS where, string name)
        {
            return where.AddWordClass(name);
        }

        public void RemoveWordClass(WordClass which)
        {
            which.GetPoS().RemoveWordClass(which);
        }

        public void RenameWordClass(WordClass which, string newName)
        {
            which.Rename(newName);
        }

        public void CreateDeclension(PoS where, string name)
        {
            where.AddDeclension(name);
        }

        public void CreateDeclension(string PoSname, string name)
        {
            foreach (PoS part in PoSList) // ALPHABETICAL MODIFY
            {
                if (part.GetName() == PoSname)
                {
                    CreateDeclension(part, name);
                    break;
                }
            }
        }

        public void RemoveDeclension(PoS where, string name)
        {
            where.RemoveDeclension(name);
        }

        public void RenameDeclension(PoS where, string name, string newName)
        {
            where.RenameDeclension(name, newName);
        }

        public void AddDeclensionFormTransformUnit(Declension dec, TransformUnit unit)
        {
            dec.AddFormTrans(unit);
        }
        public void AddDeclensionFormTransformUnit(Declension dec, string id, 
            string regex, string replace)
        {
            dec.AddFormTrans(id, regex, replace);
        }
        public void AddDeclensionPronTransformUnit(Declension dec, TransformUnit unit)
        {
            dec.AddPronTrans(unit);
        }
        public void AddDeclensionPronTransformUnit(Declension dec, string id,
            string regex, string replace)
        {
            dec.AddPronTrans(id, regex, replace);
        }
        public void AddDeclensionRhymeTransformUnit(Declension dec, TransformUnit unit)
        {
            dec.AddRhymeTrans(unit);
        }
        public void AddDeclensionRhymeTransformUnit(Declension dec, string id,
            string regex, string replace)
        {
            dec.AddRhymeTrans(id, regex, replace);
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


    static class RegexTest
    {
        public static bool UnitTest(
            string origForm,
            string IDRegex,
            string transformRegex,
            string replacement,
            string finalForm
            )
        {
            TransformUnit unit = new TransformUnit(IDRegex, transformRegex, replacement);
            CentralStorage storage = new CentralStorage();
            PoS pos = storage.AddPoS(new PoS("pos", storage));
            WordClass wc = pos.GetWordClass("pos");
            pos.AddDeclension("dec");
            Declension dec = wc.GetDeclension("dec");
            dec.AddFormTrans(unit);
            Word word = storage.AddWord(origForm, "", "", "", "", wc);
            if (word.GetWordForms()[0].GetForm() == finalForm)
            {
                return true;
            }
            else return false;
        }

        public static void Test(List<List<string>> args)
        {
            int sucCtr = 0;
            int failCtr = 0;
            int invCtr = 0;
            foreach (List<string> lst in args)
            {
                if (lst.Count != 5)
                {
                    invCtr++;
                    continue;
                }
                bool resp = UnitTest(lst[0], lst[1], lst[2], lst[3], lst[4]);
                if (resp) sucCtr++;
                else failCtr++;
            }
            Console.WriteLine(string.Format("Performed {0} tests, {1} invalid: {2} successful, {3} unsuccessful.",
                sucCtr + failCtr, invCtr, sucCtr, failCtr));
        }
    }


    static class CreateAllTest
    {
        public static void Test()
        {
            CentralStorage central = new CentralStorage();
            PoS noun = central.AddPoS("Noun");
            central.CreateDeclension(noun, "Nominative");
            central.CreateDeclension(noun, "Accusative");
            Declension nom = noun.GetWordClasses()[0].GetDeclensions()[0];
            Declension acc = noun.GetWordClasses()[0].GetDeclensions()[1];
            central.AddDeclensionFormTransformUnit(acc, "a$", "a$", "u");
            central.AddDeclensionPronTransformUnit(acc, "ʌ$", "ʌ$", "ʏ");
            central.AddDeclensionRhymeTransformUnit(acc, "a$", "a$", "u");
            Word gleira = central.AddWord("gleira", "glɪː.ɾʌ", "-ra", "fish", "", noun.GetWordClasses()[0]);
            List<WordForm> forms = gleira.GetWordForms();
            foreach (WordForm form in forms)
            {
                Console.WriteLine(form.GetForm() + " " + form.GetPronunciation() + " " + form.GetRhymeGroup().GetID());
            }
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
            CreateAllTest.Test();
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}