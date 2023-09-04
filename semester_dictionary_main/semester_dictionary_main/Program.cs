using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
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

        public override string ToString()
        {
            return "Word: " + baseForm;
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
        {
            /*
             * returns a wordform with the given form, or null if
             * such a form doesn't exist
             */
            foreach (WordForm form in forms) // ALPHABETICAL MODIFY
            {
                if (form.GetForm() == name)
                {
                    return form;
                }
            }
            return null;
        }

        public WordForm GetWordFormByDeclension(string name)
        {
            /* 
             * returns a wordform of the given declension, or null
             * if such a declension doesn't exist.
             */
            foreach (WordForm form in forms) // ALPHABETICAL MODIFY
            {
                if (form.GetDeclension().GetName() == name)
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

        public void NewDeclension(Declension declension)
        {
            WordForm newForm = new WordForm(this, declension, central);
            forms.Add(newForm);
        }

        public void RemovedDeclension(Declension declension)
        {
            foreach (WordForm form in forms)
            {
                if (form.GetDeclension() == declension)
                {
                    form.Remove();
                    forms.Remove(form);
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

        public bool CheckWordForm(string name)
        {
            List<string> formNames = forms.Select(o => o.GetForm()).ToList();
            return formNames.Contains(name);
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
                WordForm newForm = new WordForm(this, declension, central);
                forms.Add(newForm);
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

        public override string ToString()
        {
            return "Wordform: " + form;
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
            rhyme = EvaluateRhyme(DeriveForm(newBase, declension.GetRhymeTransform()));
        }

        public void EditForm(string newForm)
        {
            form = newForm;
        }

        public void EditPronunciation(string newPron)
        {
            pronunciation = newPron;
        }

        public void EditRhyme(string newRhyme)
        {
            RemoveSelfFromRhymeGroup();
            rhyme = EvaluateRhyme(newRhyme);
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

        public override string ToString()
        {
            return "Declension: " + name;
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

        public void RemoveFormTrans(TransformUnit transform)
        {
            RemoveTransform(transform, formTransform);
        }
        public void RemoveFormTrans(string identifier, string regex, string replace)
        {
            RemoveTransform(new TransformUnit(identifier, regex, replace), formTransform);
        }

        public void RemovePronTrans(TransformUnit transform)
        {
            RemoveTransform(transform, pronTransform);
        }
        public void RemovePronTrans(string identifier, string regex, string replace)
        {
            RemoveTransform(new TransformUnit(identifier, regex, replace), pronTransform);
        }

        public void RemoveRhymeTrans(TransformUnit transform)
        {
            RemoveTransform(transform, rhymeTransform);
        }
        public void RemoveRhymeTrans(string identifier, string regex, string replace)
        {
            RemoveTransform(new TransformUnit(identifier, regex, replace), rhymeTransform);
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

        public override string ToString()
        {
            return "Word Class: " + name;
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
            Declension dec = new Declension(dName, this, central);
            declensions.Add(dec);
            foreach (Word word in words)
            {
                word.NewDeclension(dec);
            }
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
                    foreach (Word word in words)
                    {
                        word.RemovedDeclension(dec);
                    }
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
        
        public bool CheckDeclension(string name)
        {
            List<string> nameList = declensions.Select(o => o.GetName()).ToList();
            return nameList.Contains(name);
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

        public override string ToString()
        {
            return "PoS: " + name;
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

        public bool CheckWordClass(string name)
        {
            List<string> nameList = wordClasses.Select(o => o.GetName()).ToList();
            return nameList.Contains(name);
        }

        public bool CheckDeclension(string name)
        {
            return declensions.Contains(name);
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

        public List<WordForm> GetRhymes()
        {
            return rhymes;
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
        // private List<Word> transList = new List<Word>();
        // ^^^ will differ from wordList once alphabetical indexing gets implemented
        // private List<WordForm> formList = new List<WordForm>(); 
        // ^^^ MAY NOT BE NEEDED: Consider, eventually delete.
        private List<PoS> PoSList = new List<PoS>();
        private List<RhymeGroup> RhymeGroupList = new List<RhymeGroup>();
        #endregion

        #region GETTERS
        public List<Word> GetWordList()
        {
            return wordList;
        }
        /*public List<Word> GetTransList()
        {
            return transList;
        }
        public List<WordForm> GetFormList()
        {
            return formList;
        }*/
        public List<PoS> GetPoSList()
        {
            return PoSList;
        }
        public List<string> GetPoSNameList()
        {
            return PoSList.Select(o => o.GetName()).ToList();
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

        public RhymeGroup GetRhymeGroup(string name)
        {
            foreach (RhymeGroup rg in RhymeGroupList) // ALPHABETICAL MODIFY
            {
                if (rg.GetID() == name)
                {
                    return rg;
                }
            }
            return null;
        }

        public List<Word> GetAllMatchingWords(string name)
        {
            List<Word> lst = new List<Word>();
            foreach (Word w in wordList) // ALPHABETICAL MODIFY
            {
                if (w.GetForm() == name)
                {
                    lst.Add(w);
                }
            }
            return lst;
        }

        public List<Word> GetAllMatchingTrans(string name)
        {
            List<Word> lst = new List<Word>();
            foreach (Word w in wordList) // ALPHABETICAL MODIFY
            {
                if (w.GetTranslation() == name)
                {
                    lst.Add(w);
                }
            }
            return lst;
        }

        public bool CheckRhymeGroup(string name)
        {
            List<string> nameList = RhymeGroupList.Select(o => o.GetID()).ToList();
            return nameList.Contains(name);
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
                    group.Insert(from);
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
            //formList.Add(form);
        }

        public void RemoveWordForm(WordForm form)
        {
            /*
             * May only be called from Word objects.
             * MAY NOT BE NEEDED: Consider, eventually delete.
             */
            //formList.Remove(form);
        }

        public Word AddWord(Word word)
        {
            wordList.Add(word);
            //transList.Add(word);
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

        public Word AddWord(string form, string pronunciation, string rhyme,
            string translation, WordClass wClass)
        {
            return AddWord(new Word(form, pronunciation, rhyme, translation, "",
                wClass, this));
        }

        public void RemoveWord(Word word)
        {
            wordList.Remove(word);
            //transList.Remove(word);
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

        public void EditWordForm(Word word, string newForm)
        {
            word.ChangeForm(newForm);
        }

        public void EditWordPronunciation(Word word, string newPronunciation)
        {
            word.ChangePronunciation(newPronunciation);
        }

        public void EditWordRhyme(Word word, string newRhyme)
        {
            word.ChangeRhyme(newRhyme);
        }

        public void EditWordTranslation(Word word, string newTranslation)
        {
            word.ChangeTranslation(newTranslation);
        }

        public void EditWordDefinition(Word word, string newDefinition)
        {
            word.ChangeDefinition(newDefinition);
        }

        public void EditWordClass(Word word, WordClass newClass)
        {
            word.ChangeClass(newClass);
        }

        public Word GetWord(string name)
        {
            /*
             * Returns the first word with the given name.
             */
            foreach (Word word in wordList) // ALPHABETICAL MODIFY
            {
                if (word.GetForm() == name)
                {
                    return word;
                }
            }
            return null;
        }

        public Word GetWordByTrans(string name)
        {
            foreach (Word word in wordList) // ALPHABETICAL MODIFY
            {
                if (word.GetTranslation() == name)
                {
                    return word;
                }
            }
            return null;
        }

        public bool CheckWord(string name)
        {
            List<string> nameList = wordList.Select(o => o.GetForm()).ToList();
            return nameList.Contains(name);
        }

        public bool CheckTrans(string name)
        {
            List<string> nameList = wordList.Select(o => o.GetTranslation()).ToList();
            return nameList.Contains(name);
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

        public bool CheckPoS(string name)
        {
            List<string> nameList = PoSList.Select(o => o.GetName()).ToList();
            return nameList.Contains(name);
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

        public void RemoveDeclensionFormTransformUnit(Declension dec, TransformUnit unit)
        {
            dec.RemoveFormTrans(unit);
        }
        public void RemoveDeclensionFormTransformUnit(Declension dec, string id,
            string regex, string replace)
        {
            dec.RemoveFormTrans(id, regex, replace);
        }
        public void RemoveDeclensionPronTransformUnit(Declension dec, TransformUnit unit)
        {
            dec.RemovePronTrans(unit);
        }
        public void RemoveDeclensionPronTransformUnit(Declension dec, string id,
            string regex, string replace)
        {
            dec.RemovePronTrans(id, regex, replace);
        }
        public void RemoveDeclensionRhymeTransformUnit(Declension dec, TransformUnit unit)
        {
            dec.RemoveRhymeTrans(unit);
        }
        public void RemoveDeclensionRhymeTransformUnit(Declension dec, string id,
            string regex, string replace)
        {
            dec.RemoveRhymeTrans(id, regex, replace);
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


    public class CommandCentre
    {
        #region ATTRIBUTES AND CONSTRUCTOR

        //private Dictionary<string, string[]> Aliases = new Dictionary<string, string[]>();

        private CentralStorage central;
        private object? focus;
        private Declension? focusedTrUParent;
        private string? focusedTrUSection;

        private Dictionary<string, Action<string[]>> cmdFuncs = new Dictionary<string, Action<string[]>>()
        {
            
        };


        public CommandCentre(CentralStorage central)
        {
            this.central = central;
            cmdFuncs["addpos"] = AddPoS;
            cmdFuncs["rmpos"] = RmPoS;
            cmdFuncs["rnmpos"] = RnmPoS;
            cmdFuncs["lspos"] = LsPoS;
            cmdFuncs["lsposw"] = LsPoSWrds;
            cmdFuncs["addwc"] = AddWC;
            cmdFuncs["rmwc"] = RmWC;
            cmdFuncs["rnmwc"] = RnmWC;
            cmdFuncs["lswc"] = LsWC;
            cmdFuncs["adddc"] = AddDc;
            cmdFuncs["rmdc"] = RmDc;
            cmdFuncs["rnmdc"] = RnmDc;
            cmdFuncs["lsdc"] = LsDc;
            cmdFuncs["addw"] = AddW;
            cmdFuncs["rmw"] = RmW;
            cmdFuncs["edw"] = EdW;
            cmdFuncs["edwtr"] = EdWTrn;
            cmdFuncs["edwdef"] = EdWDef;
            cmdFuncs["edwcls"] = EdWCls;
            cmdFuncs["lsw"] = LsW;
            cmdFuncs["rhm"] = Rhm;
            cmdFuncs["lswf"] = LsWfm;
            cmdFuncs["edwf"] = EdWfm;
            cmdFuncs["addtu"] = AddTr;
            cmdFuncs["rmtu"] = RmTr;
            cmdFuncs["lstu"] = LsTr;
            cmdFuncs["focus"] = Focus;
            cmdFuncs["help"] = Help;
        }

        #endregion

        #region PUBLIC METHODS

        public void WaitForCommand()
        {
            /*
             * Waits for a command in the terminal.
             * Blocking function.
             */
            Console.Write("> ");
            CallCommand(Console.ReadLine());
            
        }

        public void CallCommand(string line)
        {
            /* Decodes a command and calls the appropriate function.
             * Split from WaitForCommand for the purpose of unit testing.
             */
            string[] args = line.Split(" ");
            string command = args[0].ToLower();
            args = args.Skip(1).ToArray();

            // decode command

            if (cmdFuncs.ContainsKey(command))
            {
                cmdFuncs[command](args);
            }
            else
            {
                Console.WriteLine(string.Format("Unknown command '{0}'. Type 'help' for a list of commands.", command));
            }
        }

        #endregion

        #region AUXILIARY PRIVATE METHODS

        private string ConcatenateList(List<string> lst)
        {
            string str = "";
            for (int i = 0; i < lst.Count - 1; i++)
            {
                str += lst[i] + ", ";
            }
            if (lst.Count > 0)
            {
                str += lst.Last();
            }
            return str;
        }

        private void ListFocus()
        {
            if (focus == null)
            {
                Console.WriteLine("No object in focus.");
            }
            else
            {
                Console.WriteLine(focus.ToString());
            }
        }

        private void ListFormTrUs(Declension dec)
        {
            Console.WriteLine(string.Format("Form transform units of {0} ({1}):", dec.GetName(), dec.GetWordClass().GetName()));
            ListTrUs(dec.GetFormTransform());
        }
        private void ListPronTrUs(Declension dec)
        {
            Console.WriteLine(string.Format("Pronunciation transform units of {0} ({1}):", dec.GetName(), dec.GetWordClass().GetName()));
            ListTrUs(dec.GetPronunciationTransform());
        }
        private void ListRhymeTrUs(Declension dec)
        {
            Console.WriteLine(string.Format("Rhyme transform units of {0} ({1}):", dec.GetName(), dec.GetWordClass().GetName()));
            ListTrUs(dec.GetRhymeTransform());
        }

        private void ListTrUs(List<TransformUnit> tr)
        {
            foreach (TransformUnit unit in tr)
            {
                Console.WriteLine(string.Format("· Id: [ {0} ], Regex: [ {1} ], Replace with: [ {2} ] ", unit.identifier, unit.regex, unit.replace));
            }
        }

        private void FocusParent()
        {
            // shift focus to the parent, or decline if top-level.
            if (focus != null)
            {
                if (focus.GetType() == typeof(Declension))
                {
                    focus = ((Declension)focus).GetWordClass();
                    Console.WriteLine(string.Format("Now in focus: {0} (word class)", ((WordClass)focus).GetName()));
                }
                else if (focus.GetType() == typeof(WordClass))
                {
                    focus = ((WordClass)focus).GetPoS();
                    Console.WriteLine(string.Format("Now in focus: {0} (part of speech)", ((PoS)focus).GetName()));
                }
                else if (focus.GetType() == typeof(WordForm))
                {
                    focus = ((WordForm)focus).GetParent();
                    Console.WriteLine(string.Format("Now in focus: {0} (word)", ((Word)focus).GetForm()));
                }
                else
                {
                    Console.WriteLine("The object in focus has no parent.");
                }
            }
            else
            {
                Console.WriteLine("No object in focus.");
            }
        }

        private void FocusTransformUnit(string arg)
        {
            /*
             * Needs a declension in focus.
             * Lists transform units in the focused Declension, with numbers
             * Prompts the user to write one of the numbers
             * If the number is valid, focuses on that transform unit.
             * Takes one argument
             */

            string[] formAliases = { "form", "frm", "fm", "f" };
            string[] pronAliases = { "pronunciation", "pron", "pn", "p" };
            string[] rhymeAliases = { "rhyme", "rhm", "r" };

            if (focus != null && focus.GetType() == typeof(Declension))
            {
                List<TransformUnit> ls;
                string tempSignifier;

                if (formAliases.Contains(arg.ToLower()))
                {
                    ls = ((Declension)focus).GetFormTransform();
                    tempSignifier = "f";
                }
                else if (formAliases.Contains(arg.ToLower()))
                {
                    ls = ((Declension)focus).GetPronunciationTransform();
                    tempSignifier = "p";
                }
                else if (formAliases.Contains(arg.ToLower()))
                {
                    ls = ((Declension)focus).GetRhymeTransform();
                    tempSignifier= "r";
                }
                else
                {
                    Console.WriteLine(string.Format("Unknown argument {0}.\nType 'help focus' for more info.", arg));
                    return;
                }

                Console.WriteLine("Please enter the number of the transform unit you'd like to select:");
                for (int i = 0; i < ls.Count(); i++)
                {
                    Console.WriteLine(string.Format("[{0}] Id: {1}; Regex: {2}; Replace: {3}", i, ls[i].identifier, ls[i].regex, ls[i].replace));
                }

                string numStr = Console.ReadLine();
                int numInt = -1;
                while (!int.TryParse(numStr, out numInt) || !(numInt > 0 && numInt < ls.Count() - 1))
                {
                    Console.WriteLine(string.Format("Entered value was not an integer between 0 and {0}. Please enter the integer again.", ls.Count() - 1));
                    numStr = Console.ReadLine();
                }
                // numInt should contain the correct number from TryParse. If not, parse again.
                focusedTrUParent = ((Declension)focus);
                focus = ls[numInt];
                focusedTrUSection = tempSignifier;
                Console.WriteLine(string.Format("Now in focus: transform unit {0}; {1}; {2}", ((TransformUnit)focus).identifier, ((TransformUnit)focus).regex, ((TransformUnit)focus).replace));
            }
            else
            {
                Console.WriteLine("Needs a declension in focus.\nType 'help focus' for more info.");
            }
        }

        private void FocusPoS(string arg)
        {
            // check if PoS exists (thru central)
            if (!central.CheckPoS(arg))
            {
                Console.WriteLine(string.Format("Part of speech {0} not found.", arg));
            }
            else
            {
                // focus on the PoS
                focus = central.GetPoS(arg);
                Console.WriteLine(string.Format("Now in focus: {0}", arg));
            }
        }

        private void FocusWClass(string arg)
        {
            // check if focus is PoS
            if (focus != null && focus.GetType() == typeof(PoS))
            {
                // check if wclass is in PoS (through PoS)
                if (((PoS)focus).CheckWordClass(arg))
                {
                    // focus on the wclass
                    focus = ((PoS)focus).GetWordClass(arg);
                    Console.WriteLine(string.Format("Now in focus: {0}", ((WordClass)focus).GetName()));
                }
                else
                {
                    Console.WriteLine(string.Format("Word class {0} not found in part of speech {1}", arg, ((PoS)focus).GetName()));
                }
            }
            else
            {
                Console.WriteLine("A part of speech must be in focus.\nType 'help focus' for more info.");
            }
        }

        private void FocusDec(string arg)
        {
            // check if focus is Wclass
            if (focus != null && focus.GetType() == typeof(WordClass))
            {
                // check if declension is in Wclass (through Wclass)
                if (((WordClass)focus).CheckDeclension(arg))
                {
                    // focus on the declension
                    focus = ((WordClass)focus).GetDeclension(arg);
                    Console.WriteLine(string.Format("Now in focus: {0}", ((Declension)focus).GetName()));
                }
                else
                {
                    Console.WriteLine(string.Format("Declension {0} not found in word class {1}", arg, ((WordClass)focus).GetName()));
                }
            }
            else
            {
                Console.WriteLine("A word class must be in focus.\nType 'help focus' for more info.");
            }
        }

        private void FocusWord(string arg)
        {
            // check if word exists (thru central)
            if (!central.CheckWord(arg))
            {
                Console.WriteLine(string.Format("Word {0} not found.", arg));
            }
            else
            {
                // focus on the word
                List<Word> words = central.GetAllMatchingWords(arg);
                if (words.Count == 0)
                {
                    Console.WriteLine(string.Format("No words matching '{0}' found.", arg));
                }
                else if (words.Count == 1)
                {
                    focus = words.First();
                    Console.WriteLine(string.Format("Now in focus: {0}", arg));
                }
                else
                {
                    Console.WriteLine(string.Format("Found multiple words matching the form '{0}'.", arg));
                    Console.WriteLine("Please enter the number of the word you'd like to select:");
                    for (int i = 0; i < words.Count; i++)
                    {
                        if (words[i].GetDefinition() == "")
                        {
                            Console.WriteLine(string.Format("[{0}] {1} ({2}): {3}.", i, words[i].GetForm(), words[i].GetPartOfSpeech().GetName(), words[i].GetTranslation()));
                        }
                        else
                        {
                            Console.WriteLine(string.Format("[{0}] {1} ({2}): {3}. Definition: {4}", i, words[i].GetForm(), words[i].GetPartOfSpeech().GetName(), words[i].GetTranslation(), words[i].GetDefinition()));
                        }
                    }
                    string numStr = Console.ReadLine();
                    int numInt = -1;
                    while (!int.TryParse(numStr, out numInt) || !(numInt > 0 && numInt < words.Count() - 1))
                    {
                        Console.WriteLine(string.Format("Entered value was not an integer between 0 and {0}. Please enter the integer again.", words.Count() - 1));
                        numStr = Console.ReadLine();
                    }
                    // numInt should contain the correct number from TryParse. If not, parse again.
                    focus = words[numInt];
                    Console.WriteLine(string.Format("Now in focus: {0}", ((Word)focus).GetForm()));
                }
            }
        }

        private void FocusNative(string arg)
        {
            // check if word exists (thru central)
            if (!central.CheckTrans(arg))
            {
                Console.WriteLine(string.Format("Native word {0} not found.", arg));
            }
            else
            {
                // focus on the word
                focus = central.GetWordByTrans(arg);
                Console.WriteLine(string.Format("Now in focus: {0}", arg));
            }
        }

        private void FocusWForm(string arg)
        {
            /*
             * Focuses on a wordform of a word, corresponding to a declension.
             */

            // check if focus is a word
            if (focus != null && focus.GetType() == typeof(Word))
            {
                // check if the declension exists (thru PoS)
                if (((Word)focus).GetPartOfSpeech().CheckDeclension(arg))
                {
                    // focus on the wform
                    focus = ((Word)focus).GetWordFormByDeclension(arg);
                    Console.WriteLine(string.Format("Now in focus: {0} ({1} of {2})", ((WordForm)focus).GetForm(), ((WordForm)focus).GetDeclension().GetName(), ((WordForm)focus).GetParent().GetForm()));
                }
                else
                {
                    Console.WriteLine(string.Format("Declension {0} not found under part of speech {1}.", arg, ((Word)focus).GetPartOfSpeech().GetName()));
                }
            }
            else
            {
                Console.WriteLine("A word must be in focus.\nType 'help focus' for more info.");
            }
        }

        /*
        private void FocusRhymeGroup(string arg)
        {
            // check if the rhymegroup exists (thru central)
            if (!central.CheckRhymeGroup(arg))
            {
                Console.WriteLine(string.Format("Rhyme Group {0} not found.", arg));
            }
            else
            {
                // focus on the rhymegroup
                focus = central.GetRhymeGroup(arg);
                Console.WriteLine(string.Format("Now in focus: {0}", arg));
            }
        }
        */

        #endregion

        #region COMMANDS

        #region General Commands

        private void Focus(string[] args)
        {
            /*
             * Focuses on an object.
             * Focus can make it easier to type certain commands, 
             * as extra arguments can be left out.
             * Some commands cannot be run without the proper object in focus.
             * Takes 0, 1, or 2 arguments.
             * 0 args : name the object in focus.
             * 1 arg : 
             * · parent : focus on the parent of the current focus, 
             *            or unfocus if the focused object has no parent.
             * · list : name the object in focus.
             * 2 args : (nameOf arguments to be substituted with object names)
             * · pos nameOfPoS : focus on a Part of Speech.
             * · wc nameOfWC : focus on a WordClass. Can only be run with a PoS in focus.
             * · dc nameOfDec : focus on a Declension. Can only be run with a PoS in focus.
             * · w nameOfWord : focus on a Word using the target language form.
             * · nt nameOfWord : focus on a Word using the native language form.
             * · wf nameOfDeclension : focus on a WordForm. Can only be run with a Word in focus.
             *                         Note that the name given is not the worform's form, but
             *                         instead the declension under which it falls.
             * · tr f/p/r : focus on a transform unit. Can only be run with a Declension
             *              in focus. Gives a dialogue in which the user selects the transform 
             *              unit. the second argument determines whether to select from form,
             *              pronunciation, or rhyme transform units.
             */
            string[] parentAliases = { "parent", "par", "pr" };
            string[] listAliases = { "list", "ls" };
            string[] transformAliases = { "trunit", "tru", "tr", "tu" };
            string[] posAliases = { "pos" };
            string[] wClassAliases = { "wclass", "wc", "class", "cls" };
            string[] decAliases = { "declension", "dec", "dc" };
            string[] wordAliases = { "word", "wrd", "w" };
            string[] nativeAliases = { "native", "nat", "nt" };
            string[] wFormAliases = { "wordform", "wform", "wf" };
            //string[] rhymeGroupAliases = { "rhymegroup", "rgroup", "rgrp", "rgp", "rg" };

            if (args.Length == 0)
            {
                ListFocus();
            }
            else if (args.Length == 1)
            {
                if (parentAliases.Contains(args[0].ToLower()))
                {
                    FocusParent();
                }
                else if (listAliases.Contains(args[0].ToLower()))
                {
                    ListFocus();
                }
                else
                {
                    Console.WriteLine("Invalid argument. Type 'help focus' for more info.");
                }
            }
            else if (args.Length == 2)
            {
                if (posAliases.Contains(args[0].ToLower()))
                {
                    FocusPoS(args[1]);
                }

                else if (wClassAliases.Contains(args[0].ToLower()))
                {
                    FocusWClass(args[1]);
                }

                else if (decAliases.Contains(args[0].ToLower()))
                {
                    FocusDec(args[1]);
                }

                else if (wordAliases.Contains(args[0].ToLower()))
                {
                    FocusWord(args[1]);
                }

                else if (nativeAliases.Contains(args[0].ToLower()))
                {
                    FocusNative(args[1]);
                }

                else if (wFormAliases.Contains(args[0].ToLower()))
                {
                    FocusWForm(args[1]);
                }
                else if (transformAliases.Contains(args[0].ToLower()))
                {
                    FocusTransformUnit(args[1]);
                }

                /*else if (rhymeGroupAliases.Contains(args[0].ToLower()))
                {
                    FocusRhymeGroup(args[1]);
                }*/

                else
                {
                    Console.WriteLine("Invalid argument. Type 'help focus' for more info.");
                }
            }
        }

        private void Help(string[] args)    
        {
            /*
             * Helps the user with the commands.
             * Takes 0 to 1 arguments.
             * With 0 arguments, lists the available commands and their general use.
             * With a command name as an argument, gives info on how to use the 
             * command.
             */

            Dictionary<string, Dictionary<string, string>> cmdHelp = new Dictionary<string, Dictionary<string, string>>(){
                {
                    "addpos", new Dictionary<string, string>()
                    {
                        { "short" , "Adds new parts of speech."},
                        { "args", "Takes at least 1 argument: the names of the new parts of speech." },
                        { "focus", "This command does not incorporate focus." },
                        { "note", "The command will create as many part of speech as it receives arguments." }
                    }
                },
                {
                    "rmpos" , new Dictionary<string, string>()
                    {
                        { "short", "Removes parts of speech given as arguments." },
                        { "args", "Takes at least 1 argument when a PoS isn't in focus: the names of the PoS to be removed." },
                        { "focus", "When a PoS is in focus, can be run without an argument. In that case, the focused PoS will be removed." },
                        { "note", "Multiple parts of speech can be removed at once." }
                    }
                },
                {
                    "rnmpos", new Dictionary<string, string>()
                    {
                        { "short" , "Renames a part of speech."},
                        { "args", "Takes 2 arguments: the old name (for identification) and the new name." },
                        { "focus", "If a PoS is in focus, can be run with just 1 argument: the new name." },
                        { "note", "" }
                    }
                },
                {
                    "lspos", new Dictionary<string, string>()
                    {
                        { "short" , "Lists all parts of speech."},
                        { "args", "Takes no arguments." },
                        { "focus", "This command does not incorporate focus." },
                        { "note", "" }
                    }
                },
                {
                    "lsposw", new Dictionary<string, string>()
                    {
                        { "short" , "Lists all words of a part of speech."},
                        { "args", "Takes 1 argument: The name of the PoS." },
                        { "focus", "If a PoS is in focus, can be run with no arguments." },
                        { "note", "" }
                    }
                },
                {
                    "addwc", new Dictionary<string, string>()
                    {
                        { "short" , "Adds new word classes to a part of speech."},
                        { "args", "Takes at least one argument: the names of the new word classes." },
                        { "focus", "A part of speech in focus is required to run this command." },
                        { "note", "The command will create as many word classes as it receives arguments." }
                    }
                },
                {
                    "rmwc", new Dictionary<string, string>()
                    {
                        { "short" , "Removes word classes given as arguments from a part of speech."},
                        { "args", "Takes at least one argument: the names of all word classes to be removed." },
                        { "focus", "A part of speech in focus is required to run this command." },
                        { "note", "Multiple word classes can be removed at once." }
                    }
                },
                {
                    "rnmwc", new Dictionary<string, string>()
                    {
                        { "short" , "Renames a given word class."},
                        { "args", "Takes 1 argument if a word class is in focus (the new name), or 2 arguments if a part of speech is in focus. (the old name, the new name)" },
                        { "focus", "Either a part of speech or a wordclass in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "lswc", new Dictionary<string, string>()
                    {
                        { "short" , "Lists all word classes of a part of speech."},
                        { "args", "Takes no arguments." },
                        { "focus", "A part of speech in focus is required to run this command." },
                        { "note", "In each part of speech, you will see a word class with a name identical to the part of speech, in addition to your own custom word classes. This is because a part of speech cannot function without at least one word class. Feel free to delete this word class after introducing your own." }
                    }
                },
                {
                    "adddc", new Dictionary<string, string>()
                    {
                        { "short" , "Adds new declensions to a part of speech."},
                        { "args", "Takes at least 1 argument: the names of all declensions to be created." },
                        { "focus", "A part of speech in focus is required to run this command." },
                        { "note", "Adding declensions to a part of speech adds individual declensions to all of its word classes.\nThe declensions start out blank, don't forget to add transform units to each declension in each word class." }
                    }
                },
                {
                    "rmdc", new Dictionary<string, string>()
                    {
                        { "short" , "Removes a declension from a part of speech."},
                        { "args", "Takes at least one argument: the names of the declensions to be removed." },
                        { "focus", "A part of speech in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "rnmdc", new Dictionary<string, string>()
                    {
                        { "short" , "Renames a declension in a part of speech."},
                        { "args", "Takes 2 arguments: The old name (for identification) and the new name." },
                        { "focus", "A part of speech in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "lsdc", new Dictionary<string, string>()
                    {
                        { "short" , "Lists all declensions in a part of speech, or a word class."},
                        { "args", "Takes no arguments." },
                        { "focus", "Either a part of speech or a word class in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "addw", new Dictionary<string, string>()
                    {
                        { "short" , "Creates a new word."},
                        { "args", "Takes 6 or more arguments:\nbase form,\nbase pronunciation,\nbase rhyme pattern,\ntranslation,\npart of speech,\nword class.\nThe rest of the arguments will be concatenated into a definition." },
                        { "focus", "This command does not incorporate focus." },
                        { "note", "The translation of the word must NOT contain spaces." }
                    }
                },
                {
                    "rmw", new Dictionary<string, string>()
                    {
                        { "short" , "Removes a word."},
                        { "args", "Takes no arguments." },
                        { "focus", "A word in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "edw", new Dictionary<string, string>()
                    {
                        { "short" , "Edits a word's base form, pronunciation, and rhyme pattern."},
                        { "args", "Takes 3 arguments: new base form, new base pronunciation, new base rhyme pattern." },
                        { "focus", "A word in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "edwtr", new Dictionary<string, string>()
                    {
                        { "short" , "Edits a word's translation"},
                        { "args", "Takes 1 argument: the new translation." },
                        { "focus", "A word in focus is required to run this command." },
                        { "note", "The translation must NOT contain spaces." }
                    }
                },
                {
                    "edwdef", new Dictionary<string, string>()
                    {
                        { "short" , "Edits a word's definition."},
                        { "args", "This word takes at least one argument: the new definition" },
                        { "focus", "A word in focus is required to run this command." },
                        { "note", "The definition is formed by concatenating all arguments, therefore it can contain spaces." }
                    }
                },
                {
                    "edwcls", new Dictionary<string, string>()
                    {
                        { "short" , "Changes a word's word class within its part of speech."},
                        { "args", "Takes 1 argument: the target word class' name." },
                        { "focus", "A word in focus is required to run this command." },
                        { "note", "The target word class must exist within the word's part of speech.\nIt is not currently possible to change a word's part of speech." }
                    }
                },
                {
                    "lsw", new Dictionary<string, string>()
                    {
                        { "short" , "Lists all words in a dictionary style."},
                        { "args", "Takes no arguments." },
                        { "focus", "This command does not incorporate focus." },
                        { "note", "Shows the word's base form, pronuncitaion, part of speech, and word class." }
                    }
                },
                {
                    "rhm", new Dictionary<string, string>()
                    {
                        { "short" , "Shows which word forms rhyme with a given word form."},
                        { "args", "Takes no arguments." },
                        { "focus", "A word form in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "lswf", new Dictionary<string, string>()
                    {
                        { "short" , "Lists all word forms of a word."},
                        { "args", "Takes no arguments." },
                        { "focus", "A word in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "edwf", new Dictionary<string, string>()
                    {
                        { "short" , "Edits a word form's form, pronunciation and rhyme pattern."},
                        { "args", "Takes 3 arguments: the new form, new pronunciation, new rhyme pattern." },
                        { "focus", "A word form in focus is required to run this command." },
                        { "note", "This overrides the defaults derived from the word form's parent word, allowing for irregular word forms." }
                    }
                },
                {
                    "addtu", new Dictionary<string, string>()
                    {
                        { "short" , "Adds a new transform unit to a declension."},
                        { "args", "Takes 4 arguments: type, identifier (determines whether a word form get affected), regex (identifies the part of the form to replace), replace (string which replaces the pattern identified by 'regex')." },
                        { "focus", "A declension in focus is required to run this command." },
                        { "note", "The first argument - type - must be one of three letters: f, p, or r. This determines whether the new transform unit gets added to the declension's form, pronunciation, or rhyme pattern transformations." }
                    }
                },
                {
                    "rmtu", new Dictionary<string, string>()
                    {
                        { "short" , "Removes a transform unit from a declension."},
                        { "args", "Takes no arguments." },
                        { "focus", "A transform unit in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "lstu", new Dictionary<string, string>()
                    {
                        { "short" , "Lists all transform units of a declension."},
                        { "args", "Takes 0 to 1 arguments. If an argument is given, it needs to be one of three letters: f, p, or r. It determines whether the command lists the form, pronunciation, or rhyme pattern transform units. If no argument is given, it lists all of them." },
                        { "focus", "A declension in focus is required to run this command." },
                        { "note", "" }
                    }
                },
                {
                    "focus", new Dictionary<string, string>()
                    {
                        { "short" , "Puts an object in focus."},
                        { "args", "Takes 0, 1, or 2 arguments.\n0 arguments:\n· Names the object currently in focus.\n1 argument:\n· parent: switches focus to the current focus' parent, if it exists.\n2 arguments (arguments in brackets to be replaced with object names):\n· pos <name>: focus on a part of speech.\n· wc <name>: focus on a word class. Requires focus on a PoS.\n· dc <name>: focus on a declension. Requires focus on a word class.\n· w <word>: focus on a word using its base form.\n· nt <word> focus on a word using its translation to your native language.\n· wf <declension name>: focus on a word form using the name of its declension. Requires a word in focus.\n· tr <f/p/r>: focus on a transform unit. only write one letter our of the three. It determines, whether you're choosing from form, pronunciation, or rhyme pattern transform units. Requires focus on a declension." },
                        { "focus", "Some objects require prior focus on their parents." },
                        { "note", "Some commands are easier to use when an object is in focus. Others require focus to even function. You can think of focus as a directory structure." }
                    }
                },
            };
            if (args.Length == 0)
            {
                Console.WriteLine("Write commands to operate the application.\nSome commands take arguments. Separate them with a space, without commas.");
                Console.WriteLine("List of all commands:");
                foreach (KeyValuePair<string, Dictionary<string, string>> kv in cmdHelp)
                {
                    Console.WriteLine(string.Format("· {0}: {1}", kv.Key, kv.Value["short"]));
                }
                Console.WriteLine("Type 'help' with the name of a command as an argument to get more information about it.");
                Console.WriteLine("Scroll up to see all commands and basic instructions.");
            }
            else if (args.Length == 1)
            {
                if (cmdHelp.Keys.Contains(args[0].ToLower()))
                {
                    Console.WriteLine(string.Format("{0}\n\n{1}\n\n{2}\n\n{3}\n\n{4}", args[0].ToLower(), cmdHelp[args[0].ToLower()]["short"], cmdHelp[args[0].ToLower()]["args"], cmdHelp[args[0].ToLower()]["focus"], cmdHelp[args[0].ToLower()]["note"]));
                }
                else
                {
                    Console.WriteLine(string.Format("Unknown command {0}", args[0].ToLower()));
                }
            }
            else
            {
                Console.WriteLine("Type 'help' without arguments to get a list of commands. The help command only takes one argument at maximum.");
            }
        }

        #endregion

        #region PoS Commands

        private void AddPoS(string[] args)
        {
            /*
             * Adds new Parts of Speech.
             * Takes n arguments: the names of all new Parts of Speech to be added.
             */
            if (args.Length == 0)
            {
                Console.WriteLine("Missing arguments. Correct useage: addpos name1 name2 name3 ...\nType 'help addpos' for more detail.");
                return;
            }
            List<string> success = new List<string>();
            List<string> alreadyExists = new List<string>();
            foreach (string s in args)
            {
                if (central.CheckPoS(s))
                {
                    alreadyExists.Add(s);
                }
                else
                {
                    central.AddPoS(s);
                    success.Add(s);
                } 
            }

            string succString = ConcatenateList(success);

            string alrString = ConcatenateList(alreadyExists);

            if (success.Count > 0)
            {
                Console.WriteLine(string.Format("Succesfully added part(s) of speech {0}.", succString));
            }
            if (alreadyExists.Count > 0)
            {
                Console.WriteLine(string.Format("Part(s) of speech {0} already existed.", alrString));
            }
        }

        private void RmPoS(string[] args)
        {
            /*
             * Removes all given Parts of Speech.
             * Takes n arguments: the names of the PoS to be removed.
             * If a PoS is in focus, can be run with no arguments.
             */
            if (args.Length == 0)
            {
                if (focus != null && focus.GetType() == typeof(PoS))
                {
                    string[] newArgs = { ((PoS)focus).GetName() };
                    RmPoS(newArgs);
                }
                else
                {
                    Console.WriteLine("Missing arguments. Correct useage: rmpos name1 name2 name3 ...\nType 'help rmpos' for more detail.");
                    return;
                }
            }

            List<string> success = new List<string>();
            List<string> doesntExist = new List<string>();

            foreach (string s in args)
            {
                if (!central.CheckPoS(s)) {
                    doesntExist.Add(s);
                }
                else
                {
                    central.RemovePoS(central.GetPoS(s));
                    success.Add(s);
                }
            }

            string succString = ConcatenateList(success);
            string dexString = ConcatenateList(doesntExist);

            if (success.Count > 0)
            {
                Console.WriteLine(string.Format("Successfully removed part(s) of speech {0}.", succString));

            }
            if (doesntExist.Count > 0)
            {
                Console.WriteLine(string.Format("Part(s) of speech {0} were not found.", dexString));
            }
        }

        private void RnmPoS(string[] args)
        {
            /*
             * Renames the given part of speech.
             * Takes 2 arguments: original PoS name, and a new name.
             * If a PoS is in focus, can be run with only 1 argument (the new name).
             */
            if (args.Length == 1 && focus != null && focus.GetType() == typeof(PoS))
            {
                string[] newArgs = { ((PoS)focus).GetName(), args[0] };
                RnmPoS(newArgs);
            }
            if (args.Length != 2)
            {
                Console.WriteLine("Requires 2 arguments. Correct useage: rnmpos oldname newname\nType 'help rnmpos' for more detail.");
            }
            else if (!central.CheckPoS(args[0]))
            {
                Console.WriteLine(string.Format("Part of speech {0} not found. Did you spell it correctly?", args[0]));
            }
            else if (central.CheckPoS(args[1]))
            {
                Console.WriteLine(string.Format("Part of speech under the name {0} already exists.", args[1]));
            }
            else
            {
                central.RenamePoS(central.GetPoS(args[0]), args[1]);
                Console.WriteLine(string.Format("Part of speech {0} renamed to {1}", args[0], args[1]));
            }
        }

        private void LsPoS(string[] args)
        {
            /*
             * Lists the Parts of Speech in the language.
             * Takes no arguments.
             */
            Console.WriteLine(ConcatenateList(central.GetPoSNameList()));
        }

        private void LsPoSWrds(string[] args)
        {
            /*
             * Lists all the words in the given part of speech.
             * Can be run through focus or with an argument.
             * Giving an argument overrides focus.
             * Running without an argument and without a PoS in focus is unsuccessful.
             */

            if (args.Length == 1)
            {
                if (central.CheckPoS(args[0]))
                {
                    List<string> wList = central.GetPoS(args[0]).GetWords().Select(o => o.GetForm()).ToList();
                    Console.WriteLine(ConcatenateList(wList));
                }
                else
                {
                    Console.WriteLine(string.Format("Part of speech {0} not found.", args[0]));
                }
            }

            else if (args.Length > 1)
            {
                Console.WriteLine("Too many arguments: This command takes at most 1 argument.");
            }

            else if (focus != null && focus.GetType() == typeof(PoS))
            {
                List<string> wList = ((PoS)focus).GetWords().Select(o => o.GetForm()).ToList();
                Console.WriteLine(ConcatenateList(wList));
            }

            else
            {
                Console.WriteLine("Incorrect useage: This command needs an argument or a part of speech in focus.\nType 'help lsposwd' for more info.");
            }
        }

        #endregion

        #region WordClass commands

        private void LsWC(string[] args)
        {
        /*
         * Lists the WordClasses contained in a PoS in focus. 
         * Takes no arguments.
         */

            // check if focus is PoS

            if (focus != null && focus.GetType() == typeof(PoS))
            {
                List<WordClass> wcs = ((PoS)focus).GetWordClasses();
                List<string> wcNames = wcs.Select(wc => wc.GetName()).ToList();
                Console.WriteLine(ConcatenateList(wcNames));
            }
            else
            {
                Console.WriteLine("A part of speech must be in focus.\nType 'help focus' for more info.");
            }
        }

        private void AddWC(string[] args)
        {
            /*
             * Adds a WordClass / several WordClasses to a PoS in focus.
             * Needs a PoS in focus.
             * Takes at least one argument: The new WordClasses' names.
             */

            if (focus != null && focus.GetType() == typeof(PoS))
            {
                if (args.Length > 0)
                {
                    List<string> succ = new List<string>();
                    List<string> fail = new List<string>();
                    foreach (string s in args)
                    {
                        if (!((PoS)focus).CheckWordClass(s))
                        {
                            central.CreateWordClass((PoS)focus, s);
                            succ.Add(s);
                        }
                        else
                        {
                            fail.Add(s);
                        }
                    }
                    if (succ.Count > 0)
                    {
                        Console.WriteLine(string.Format("Successfully added classes {0} to part of speech {1}.", ConcatenateList(succ), ((PoS)focus).GetName()));
                    }
                    if (fail.Count > 0)
                    {
                        Console.WriteLine(string.Format("Classes {0} already found in part of speech {1}.", ConcatenateList(fail), ((PoS)focus).GetName()));
                    }
                }
                else
                {
                    Console.WriteLine("Requires at least one argument.\nType 'help addwc' for more info.");
                }
            }
            else
            {
                Console.WriteLine("A part of speech must be in focus.\nType 'help focus' for more info.");
            }
        }

        private void RmWC(string[] args)
        {
            // there was a mistake here, hopefully fixed, but further examination might be needed.

            /*
             * Removes a WordClass / several WordClasses from a PoS in focus.
             * Needs a PoS or WordClass in focus.
             * If a WordClass is in focus, only it can be removed.
             * Takes at least one argument: The deleted WordClasses' names.
             */

            if (focus != null && focus.GetType() == typeof(PoS))
            {
                if (args.Length > 0)
                {
                    List<string> succ = new List<string>();
                    List<string> fail = new List<string>();
                    foreach (string s in args)
                    {
                        if (((PoS)focus).CheckWordClass(s))
                        {
                            central.RemoveWordClass(((PoS)focus).GetWordClass(s));
                            succ.Add(s);
                        }
                        else
                        {
                            fail.Add(s);
                        }
                    }
                    if (succ.Count > 0)
                    {
                        Console.WriteLine(string.Format("Successfully removed classes {0} from part of speech {1}.", ConcatenateList(succ), ((PoS)focus).GetName()));
                    }
                    if (fail.Count > 0)
                    {
                        Console.WriteLine(string.Format("Classes {0} not found in part of speech {1}.", ConcatenateList(fail), ((PoS)focus).GetName()));
                    }
                }
                else
                {
                    Console.WriteLine("Requires at least one argument when a part of speech is in focus.\nType 'help rmwc' for more info.");
                }
            }
            else if (focus != null && focus.GetType() == typeof(WordClass))
            {
                string name = ((WordClass)focus).GetName();
                central.RemoveWordClass((WordClass)focus);
                focus = null;
                Console.WriteLine("Removed class {0}", name);
            }
            else
            {
                Console.WriteLine("A part of speech or a word class must be in focus.\nType 'help focus' for more info.");
            }
        }

        private void RnmWC(string[] args)
        {
            /*
             * Renames a WordClass in focus.
             * Can work with a PoS in focus too.
             * With a WC in focus, takes one argument: the new name.
             * With a PoS in focus, takes two arguments: the old name, and the new name.
             */

            if (focus != null && focus.GetType() == typeof(WordClass))
            {
                if (args.Length == 1)
                {
                    string oldName = ((WordClass)focus).GetName();
                    central.RenameWordClass((WordClass)focus, args[0]);
                    Console.WriteLine("Successfully renamed class {0} to {1}", oldName, ((WordClass)focus).GetName());
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: takes 1 argument when a word class is in focus.\nType 'help rnmwc' for more info.");
                }
            }
            else if (focus != null && focus.GetType() == typeof(PoS))
            {
                if (args.Length == 2)
                {
                    if (((PoS)focus).CheckWordClass(args[0])) // word class exists
                    {
                        WordClass cls = ((PoS)focus).GetWordClass(args[0]);
                        string oldName = cls.GetName();
                        central.RenameWordClass(cls, args[0]);
                        Console.WriteLine("Successfully renamed class {0} to {1}", oldName, cls.GetName());
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Word class {0} not found in part of speech {1}.", args[0], ((PoS)focus).GetName()));
                    }
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: takes 2 arguments when a part of speech is in focus.\nType 'help rnmwc' for more info.");
                }
            }
        }

        #endregion

        #region Declension Commands

        private void LsDc(string[] args)
        {
            /*
             * Lists the Declensions contained in a PoS or WordClass in focus. 
             * Takes no arguments.
             */

            // check if focus is PoS

            if (focus != null && focus.GetType() == typeof(PoS))
            {
                List<string> dcs = ((PoS)focus).GetDeclensions();
                Console.WriteLine(ConcatenateList(dcs));
            }
            else if (focus != null && focus.GetType() == typeof(WordClass))
            {
                List<Declension> dcs = ((WordClass)focus).GetDeclensions();
                List<string> names = dcs.Select(o => o.GetName()).ToList();
                Console.WriteLine(ConcatenateList(names));
            }
            else
            {
                Console.WriteLine("A part of speech or a word class must be in focus.\nType 'help focus' for more info.");
            }
        }

        private void AddDc(string[] args)
        {
            /*
             * Adds a Declension / several Declensions to a PoS in focus.
             * Needs a PoS in focus.
             * Takes at least one argument: The new Declensions' names.
             */

            if (focus != null && focus.GetType() == typeof(PoS))
            {
                if (args.Length > 0)
                {
                    List<string> succ = new List<string>();
                    List<string> fail = new List<string>();
                    foreach (string s in args)
                    {
                        if (!((PoS)focus).CheckDeclension(s))
                        {
                            central.CreateDeclension((PoS)focus, s);
                            succ.Add(s);
                        }
                        else
                        {
                            fail.Add(s);
                        }
                    }
                    if (succ.Count > 0)
                    {
                        Console.WriteLine(string.Format("Successfully added declensions {0} to part of speech {1}.\nDon't forget to edit their transformations in individual word classes.", ConcatenateList(succ), ((PoS)focus).GetName()));
                    }
                    if (fail.Count > 0)
                    {
                        Console.WriteLine(string.Format("Declensions {0} already found in part of speech {1}.", ConcatenateList(fail), ((PoS)focus).GetName()));
                    }
                }
                else
                {
                    Console.WriteLine("Requires at least one argument.\nType 'help adddc' for more info.");
                }
            }
            else
            {
                Console.WriteLine("A part of speech must be in focus.\nType 'help focus' for more info.");
            }
        }

        private void RmDc(string[] args)
        {
            /*
             * Removes a Declension / several Declensions from a PoS in focus.
             * Needs a PoS or Declension in focus.
             * If a Declension is in focus, only it can be removed.
             * Takes at least one argument: The deleted Declensions' names.
             */

            if (focus != null && focus.GetType() == typeof(PoS))
            {
                if (args.Length > 0)
                {
                    List<string> succ = new List<string>();
                    List<string> fail = new List<string>();
                    foreach (string s in args)
                    {
                        if (((PoS)focus).CheckDeclension(s))
                        {
                            central.RemoveDeclension((PoS)focus, s);
                            succ.Add(s);
                        }
                        else
                        {
                            fail.Add(s);
                        }
                    }
                    if (succ.Count > 0)
                    {
                        Console.WriteLine(string.Format("Successfully removed declensions {0} from part of speech {1}.", ConcatenateList(succ), ((PoS)focus).GetName()));
                    }
                    if (fail.Count > 0)
                    {
                        Console.WriteLine(string.Format("Declensions {0} not found in part of speech {1}.", ConcatenateList(fail), ((PoS)focus).GetName()));
                    }
                }
                else
                {
                    Console.WriteLine("Requires at least one argument when a part of speech is in focus.\nType 'help rmdc' for more info.");
                }
            }
            else if (focus != null && focus.GetType() == typeof(Declension))
            {
                string name = ((Declension)focus).GetName();
                PoS parent = ((Declension)focus).GetPoS();
                central.RemoveDeclension(parent, name);
                focus = null;
                Console.WriteLine("Removed declension {0} from part of speech {1}.", name, parent);
            }
            else
            {
                Console.WriteLine("A part of speech or a declension must be in focus.\nType 'help focus' for more info.");
            }
        }

        private void RnmDc(string[] args)
        {
            /*
             * Renames a Declension in focus.
             * Can work with a PoS in focus too.
             * With a declension in focus, takes one argument: the new name.
             * With a PoS in focus, takes two arguments: the old name, and the new name.
             */

            if (focus != null && focus.GetType() == typeof(Declension))
            {
                if (args.Length == 1)
                {
                    string oldName = ((Declension)focus).GetName();
                    central.RenameDeclension(((Declension)focus).GetPoS(), oldName, args[0]);
                    Console.WriteLine("Successfully renamed declension {0} to {1}", oldName, ((Declension)focus).GetName());
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: takes 1 argument when a declension is in focus.\nType 'help rnmdc' for more info.");
                }
            }
            else if (focus != null && focus.GetType() == typeof(PoS))
            {
                if (args.Length == 2)
                {
                    if (((PoS)focus).CheckDeclension(args[0]))
                    {
                        central.RenameDeclension((PoS)focus, args[0], args[1]);
                        Console.WriteLine("Successfully renamed declension {0} to {1}", args[0], args[1]);
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Declension {0} not found in part of speech {1}.", args[0], ((PoS)focus).GetName()));
                    }
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: takes 2 arguments when a part of speech is in focus.\nType 'help rnmdc' for more info.");
                }
            }
        }

        #endregion

        #region TransformUnit commands

        private void AddTr(string[] args)
        {
            /*
             * Adds a transform unit to a declension in focus.
             * Needs a declension in focus.
             * Takes four arguments:
             * · form / pronunciation / rhyme
             *     Decides whether adds the unit to the
             *     form, pronunciation, or rhyme.
             *     Can be abbreviated as frm/fm/f, pron/pn/p, and rhm/r respectively.
             * · identifier: regex that identifies whether the word is to be affected
             *     for example a TrU with id. 'a$' will affect the word 'gleira,
             *     but not the word 'hvils'.
             * · regex: identifies the part of the word to be replaced.
             * · replace: string to replace the part of the word identified by
             * the regex.
             */
            if (focus != null && focus.GetType() == typeof(Declension))
            {
                if (args.Length == 4)
                {
                    string[] formAliases = { "form", "frm", "fm", "f" };
                    string[] pronAliases = { "pronunciation", "pron", "pn", "p" };
                    string[] rhymeAliases = { "rhyme", "rhm", "r" };
                    TransformUnit unit = new TransformUnit(args[1], args[2], args[3]);

                    if (formAliases.Contains(args[0].ToLower()))
                    {
                        central.AddDeclensionFormTransformUnit((Declension)focus, unit);
                        Console.WriteLine(string.Format("Added the transform unit to {0}", ((Declension)focus).GetName()));
                    }
                    else if (pronAliases.Contains(args[0].ToLower()))
                    {
                        central.AddDeclensionPronTransformUnit((Declension)focus, unit);
                        Console.WriteLine(string.Format("Added the transform unit to {0}", ((Declension)focus).GetName()));
                    }
                    else if (rhymeAliases.Contains(args[0].ToLower()))
                    {
                        central.AddDeclensionRhymeTransformUnit((Declension)focus, unit);
                        Console.WriteLine(string.Format("Added the transform unit to {0}", ((Declension)focus).GetName()));
                    }
                    else
                    {
                        Console.WriteLine("The first subcommand must be f, p, or r, or any of their longer versions.\nType 'help addtr' for more info.");
                    }
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: takes 4 arguments.\nType 'help addtr' for more info.");
                }
            }
            else
            {
                Console.WriteLine("Needs a declension in focus.\nType 'help focus' for more info.");
            }
        }

        private void LsTr(string[] args)
        {
            /*
             * Lists all transform units under a declension.
             * Needs a declension in focus.
             * Takes 0 to 1 arguments: f/p/r for displaying only form,
             * pronunciation, or rhyme transform units.
             * If no arguments are given, displays all in this order.
             */
            string[] formAliases = { "form", "frm", "fm", "f" };
            string[] pronAliases = { "pronunciation", "pron", "pn", "p" };
            string[] rhymeAliases = { "rhyme", "rhm", "r" };

            if (focus != null && focus.GetType() == typeof(Declension))
            {
                if (args.Length == 0)
                {
                    ListFormTrUs((Declension)focus);
                    ListPronTrUs((Declension)focus);
                    ListRhymeTrUs((Declension)focus);
                }
                else if (args.Length == 1)
                {
                    if (formAliases.Contains(args[0]))
                    {
                        ListFormTrUs((Declension)focus);
                    }
                    else if (pronAliases.Contains(args[0]))
                    {
                        ListPronTrUs((Declension)focus);
                    }
                    else if (rhymeAliases.Contains(args[0]))
                    {
                        ListRhymeTrUs((Declension)focus);
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Unknown argument {0}. The argument must be f, p, r, or any of their longer variants.\nType 'help lstr' for more info."));
                    }
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: This command takes 0 or 1 arguments.\nType 'help lstr' for more info.");
                }
            }
            else
            {
                Console.WriteLine("Needs a declension in focus.\nType 'help focus' for more info.");
            }
        }

        private void RmTr(string[] args)
        {
            /*
             * Removes a transform unit in focus.
             * Needs a transform unit in focus.
             * Takes no arguments.
             */
            if (focus != null && focus.GetType() == typeof(TransformUnit))
            {
                if (focusedTrUParent == null || focusedTrUSection == null)
                {
                    Console.WriteLine("Something went wrong with the focus function.\nResetting focus.");
                    focus = null;
                    focusedTrUParent = null;
                    focusedTrUSection = null;
                    return;
                }
                if (focusedTrUSection == "f")
                {
                    focusedTrUParent.RemoveFormTrans((TransformUnit)focus);
                    focus = null;
                    focusedTrUParent = null;
                    focusedTrUSection = null;
                }
                else if (focusedTrUSection == "p")
                {
                    focusedTrUParent.RemovePronTrans((TransformUnit)focus);
                    focus = null;
                    focusedTrUParent = null;
                    focusedTrUSection = null;
                }
                else if (focusedTrUSection == "r")
                {
                    focusedTrUParent.RemoveRhymeTrans((TransformUnit)focus);
                    focus = null;
                    focusedTrUParent = null;
                    focusedTrUSection = null;
                }
                else
                {
                    Console.WriteLine("Something went wrong with the focus function.\nResetting focus.");
                    focus = null;
                    focusedTrUParent = null;
                    focusedTrUSection = null;
                    return;
                }
            }
            else
            {
                Console.WriteLine("Needs a transform unit in focus.\nType 'help focus' for more info.");
            }
        }

        #endregion

        #region Word Commands

        private void AddW(string[] args)
        {
            /*
             * Adds a Word.
             * Takes 6 or more arguments:
             *   Base form
             *   Base pronunciation
             *   Base rhyme
             *   Translation
             *   Part of speech (name needs to exist)
             *   Word class (name needs to exist within the given PoS)
             *   Definition (voluntary) (concatenates from all trailing arguments)
             * Multiple words with the same form can exist.
             */
            if (args.Length >= 6)
            {
                if (central.CheckPoS(args[4]))
                {
                    if (central.GetPoS(args[4]).CheckWordClass(args[5]))
                    {
                        Word w;
                        if (args.Length == 6)
                        {
                            w = central.AddWord(args[0], args[1], args[2], args[3], central.GetPoS(args[4]).GetWordClass(args[5]));
                        }
                        else
                        {
                            string[] defArray = new string[args.Length - 6];
                            Array.Copy(args, 6, defArray, 0, args.Length -6);
                            string def = string.Join(" ", defArray);
                            w = central.AddWord(args[0], args[1], args[2], args[3], def, central.GetPoS(args[4]).GetWordClass(args[5]));
                        }
                        Console.WriteLine("Created word {0} meaning {1}.", w.GetForm(), w.GetTranslation());
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Word class {0} not found in part of speech {1}.", args[5], args[4]));
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("Part of speech {0} not found.", args[4]));
                }
            }
        }

        private void RmW(string[] args)
        {
            /*
             * Removes a word in focus.
             * Takes no arguments.
             */
            if (focus != null && focus.GetType() == typeof(Word))
            {
                string name = ((Word)focus).GetForm();
                central.RemoveWord((Word)focus);
                focus = null;
                Console.WriteLine(string.Format("Removed word {0}", name));
            }
        }

        private void EdW(string[] args)
        {
            /*
             * Edits the base form, pronunciation, and rhyme pattern 
             * of the focused word.
             * Needs a word in focus.
             * Takes 3 argument: The new form, pronunciation and 
             * rhyme pattern.
             */
            if (focus != null && focus.GetType() == typeof(Word))
            {
                if (args.Length == 3)
                {
                    string oldForm = ((Word)focus).GetForm();
                    string oldPron = ((Word)focus).GetPronunciation();
                    string oldRhyme = ((Word)focus).GetRhyme();
                    central.EditWordForm((Word)focus, args[0]);
                    Console.WriteLine(string.Format("Changed the word from {0} /{1}/, rhyming with {2}, to {3} /{4}/, rhyming with {5}", oldForm, oldPron, oldRhyme, args[0], args[1], args[2]));
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: This command takes 3 arguments.\nType 'help edw' for more info.");
                }
            }
            else
            {
                Console.WriteLine("Needs a word in focus.\nType 'help focus' for more info.");
            }
        }

        private void EdWTrn(string[] args)
        {
            /*
             * Edits the translation of the focused word.
             * Needs a word in focus.
             * Takes one argument: The new translation
             */
            if (focus != null && focus.GetType() == typeof(Word))
            {
                if (args.Length == 1)
                {
                    central.EditWordTranslation((Word)focus, args[0]);
                    Console.WriteLine(string.Format("Changed the translation of {0} to {1}.", ((Word)focus).GetForm(), args[0]));
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: This command takes 1 argument.\nType 'help edwtrn' for more info.");
                }
            }
            else
            {
                Console.WriteLine("Needs a word in focus.\nType 'help focus' for more info.");
            }
        }

        private void EdWDef(string[] args)
        {
            /*
             * Edits the definition of the focused word.
             * Needs a word in focus.
             * Takes one argument: The new definition
             */
            if (focus != null && focus.GetType() == typeof(Word))
            {
                central.EditWordDefinition((Word)focus, string.Join(" ", args));
                Console.WriteLine(string.Format("Changed the definition of {0} to {1}.", ((Word)focus).GetForm(), ((Word)focus).GetDefinition()));
            }
            else
            {
                Console.WriteLine("Needs a word in focus.\nType 'help focus' for more info.");
            }
        }

        private void EdWCls(string[] args)
        {
            /*
             * Changes the class of the focused word.
             * Needs a word in focus.
             * Takes one argument: The new class name
             */
            if (focus != null && focus.GetType() == typeof(Word))
            {
                if (args.Length == 1)
                {
                    if (((Word)focus).GetPartOfSpeech().CheckWordClass(args[0]))
                    {
                        central.EditWordClass((Word)focus, ((Word)focus).GetPartOfSpeech().GetWordClass(args[0]));
                        Console.WriteLine(string.Format("Changed the WordClass of {0} to {1}.", ((Word)focus).GetForm(), args[0]));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Word class {0} not found in part of speech {1}.", args[0], ((Word)focus).GetPartOfSpeech().GetName()));
                    }
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: This command takes 1 argument.\nType 'help edwtrn' for more info.");
                }
            }
            else
            {
                Console.WriteLine("Needs a word in focus.\nType 'help focus' for more info.");
            }
        }

        private void LsW(string[] args)
        {
            /*
             * Lists all words in a dictionary style.
             * Takes no arguments.
             */

            if (args.Length > 0)
            {
                Console.WriteLine("Incorrect number of arguments: This command takes no arguments.");
            }
            else
            {
                List<Word> words = central.GetWordList();
                foreach (Word word in words)
                {
                    if (word.GetPartOfSpeech().GetWordClasses().Count() > 1)
                    {
                        Console.WriteLine(string.Format("· {0} /{1}/ ({2}; {3}): {4}", word.GetForm(), word.GetPronunciation(), word.GetPartOfSpeech().GetName(), word.GetWordClass().GetName(), word.GetTranslation()));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("· {0} /{1}/ ({2}): {3}", word.GetForm(), word.GetPronunciation(), word.GetPartOfSpeech().GetName(), word.GetTranslation()));
                    }

                    if (word.GetDefinition() != "")
                    {
                        Console.WriteLine(string.Format("\tDefinition: {0}", word.GetDefinition()));
                    }
                }
            }
        }

        #endregion

        #region WordForm Commands

        private void Rhm(string[] args)
        {
            /*
             * Lists the rhymes of the wordform in focus.
             * Needs a wordform in focus, to discern words with potentially
             * identical base forms.
             * Takes no arguments.
             */
            if (focus != null && focus.GetType() == typeof(WordForm))
            {
                List<WordForm> forms = ((WordForm)focus).GetRhymeGroup().GetRhymes();
                foreach(WordForm form in forms)
                {
                    if (form.GetParent().GetPartOfSpeech().GetWordClasses().Count() > 1)
                    {
                        Console.WriteLine(string.Format("· {0} /{1}/: {2} of {3} /{4}/ ({5}; {6}): {7}", form.GetForm(), form.GetPronunciation(), form.GetDeclension().GetName(), form.GetParent().GetForm(), form.GetParent().GetPronunciation(), form.GetParent().GetPartOfSpeech().GetName(), form.GetParent().GetWordClass().GetName(), form.GetParent().GetTranslation()));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("· {0} /{1}/: {2} of {3} /{4}/ ({5}): {6}", form.GetForm(), form.GetPronunciation(), form.GetDeclension().GetName(), form.GetParent().GetForm(), form.GetParent().GetPronunciation(), form.GetParent().GetPartOfSpeech().GetName(), form.GetParent().GetTranslation()));
                    }

                    if (form.GetParent().GetDefinition() != "")
                    {
                        Console.WriteLine(string.Format("\tDefinition: {0}", form.GetParent().GetDefinition()));
                    }
                }
            }
            else
            {
                Console.WriteLine("A word needs to be in focus.\nType 'help focus' for more info.");
            }
        }

        private void LsWfm(string[] args)
        {
            /*
             * Lists the wordform of a word in focus.
             * Needs a word in focus, to discern words with
             * potentially identical base forms.
             * Takes no arguments.
             */
            if (focus != null && focus.GetType() == typeof(Word))
            {
                if (args.Length == 0)
                {
                    List<WordForm> forms = ((Word)focus).GetWordForms();
                    Console.WriteLine(string.Format("Declensions of {0}:", ((Word)focus).GetForm()));
                    foreach (WordForm form in forms)
                    {
                        Console.WriteLine(string.Format("· {0}: {1} /{2}/", form.GetDeclension().GetName(), form.GetForm(), form.GetPronunciation()));
                    }
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: This command takes no arguments.");
                }
            }
            else
            {
                Console.WriteLine("Needs a word in focus.\nType 'help focus' for more info.");
            }
        }

        private void EdWfm(string[] args)
        {
            /*
             * Edits the form, pronunciation and rhyme pattern 
             * of the WordForm, allowing for irregular declension.
             * Requires the worform in focus.
             * Takes 3 argument: the new form, pronunciation, and
             * rhyme pattern of the WordForm.
             */
            if (focus != null && focus.GetType() == typeof(WordForm))
            {
                if (args.Length == 3)
                {
                    ((WordForm)focus).EditForm(args[0]);
                    ((WordForm)focus).EditPronunciation(args[1]);
                    ((WordForm)focus).EditRhyme(args[2]);
                    Console.WriteLine(string.Format("Edited the {0} of {1} to {2} /{3}/, rhyming with {4}", ((WordForm)focus).GetDeclension().GetName(), ((WordForm)focus).GetParent().GetForm(), ((WordForm)focus).GetForm(), ((WordForm)focus).GetPronunciation(), ((WordForm)focus).GetRhymeGroup().GetID()));
                }
                else
                {
                    Console.WriteLine("Incorrect number of arguments: This command takes 3 arguments.\nType 'help edwfm for more info.");
                }
            }
            else
            {
                Console.WriteLine("Needs a word form in focus.\nType 'help focus' for more info.");
            }
        }

        #endregion

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
            // ApplicationConfiguration.Initialize();
            // Application.Run(new Form1());

            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            while (true)
            {
                cc.WaitForCommand();
            }
        }
    }
}