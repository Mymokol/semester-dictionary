using rosalinds_dictionary;

namespace TestProject1
{
    [TestClass]
    public class PoSTestClass
    {
        [TestMethod]
        public void CreatePoSTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun adjective pronoun number verb");
            string PoSNamesConcatenated = 
                string.Join(" ", central.GetPoSList().Select(o=>o.GetName()).ToList());


            // assert
            Assert.AreEqual("noun adjective pronoun number verb", PoSNamesConcatenated);
        }

        [TestMethod]
        public void RemovePoSTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun adjective pronoun number verb");
            cc.CallCommand("rmpos adjective");
            cc.CallCommand("focus pos number");
            cc.CallCommand("rmpos");

            string PoSNamesConcatenated =
                string.Join(" ", central.GetPoSList().Select(o => o.GetName()).ToList());

            // assert
            Assert.AreEqual("noun pronoun verb", PoSNamesConcatenated);
        }

        [TestMethod]
        public void RenamePoSTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun ajective pronun");
            cc.CallCommand("rnmpos ajective adjective");
            cc.CallCommand("focus pos pronun");
            cc.CallCommand("rnmpos pronoun");

            string PoSNamesConcatenated =
                string.Join(" ", central.GetPoSList().Select(o => o.GetName()).ToList());

            // assert

            Assert.AreEqual("noun adjective pronoun", PoSNamesConcatenated);
        }
    }

    [TestClass]
    public class WordClassTestClass
    {
        [TestMethod]
        public void CreateWordClassTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("addwc wordclass");
            string WCNamesConcatenated =
                string.Join(" ", central.GetPoS("noun").GetWordClasses().Select(o => o.GetName()).ToList());


            // assert
            Assert.AreEqual("noun wordclass", WCNamesConcatenated);
        }
        [TestMethod]
        public void RemoveWordClassTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("addwc feminine masculine neuter");
            cc.CallCommand("rmwc noun");
            string WCNamesConcatenated =
                string.Join(" ", central.GetPoS("noun").GetWordClasses().Select(o => o.GetName()).ToList());


            // assert
            Assert.AreEqual("feminine masculine neuter", WCNamesConcatenated);
        }
        [TestMethod]
        public void TryRemovingLastWordClassTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("rmwc noun");
            string WCNamesConcatenated =
                string.Join(" ", central.GetPoS("noun").GetWordClasses().Select(o => o.GetName()).ToList());


            // assert
            Assert.AreEqual(WCNamesConcatenated, "noun");
        }
        [TestMethod]
        public void RenameWordClassTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("addwc feminine masculin neutral");
            cc.CallCommand("rnmwc masculin masculine");
            cc.CallCommand("focus wc neutral");
            cc.CallCommand("rnmwc neuter");
            string WCNamesConcatenated =
                string.Join(" ", central.GetPoS("noun").GetWordClasses().Select(o => o.GetName()).ToList());


            // assert
            Assert.AreEqual("noun feminine masculine neuter", WCNamesConcatenated);
        }
    }

    [TestClass]
    public class WordTestClass
    {
        [TestMethod]
        public void CreateWordTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("addw gleira gli:ra eira fish noun noun");
            Word? word = central.GetWord("gleira");



            // assert
            Assert.IsNotNull(word);
        }

        [TestMethod]
        public void WordPropertiesTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("addw gleira gli:ra eira fish noun noun an animal that swims in water");
            Word word = central.GetWord("gleira");
            string PropConcatenated = word.GetForm() + "\n" + word.GetPronunciation()
                + "\n" + word.GetRhyme() + "\n" + word.GetTranslation() + "\n"
                + word.GetPartOfSpeech().GetName() + "\n" + word.GetWordClass().GetName()
                + "\n" + word.GetDefinition();



            // assert
            Assert.AreEqual("gleira\ngli:ra\neira\nfish\nnoun\nnoun\nan animal that swims in water", PropConcatenated);
        }

        [TestMethod]
        public void RemoveWordTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("addw gleira gli:ra eira fish noun noun");
            cc.CallCommand("focus w gleira");
            cc.CallCommand("rmw");
            Word? word = central.GetWord("gleira");

            // assert
            Assert.IsNull(word);
        }

        [TestMethod]
        public void EditWordTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("addwc feminine masculine neuter");
            cc.CallCommand("addw gleira gli:ra eira fish noun feminine an animal that swims in water");
            cc.CallCommand("focus w gleira");
            cc.CallCommand("edw þreitt þri:ht eitt");
            cc.CallCommand("edwtr apple");
            cc.CallCommand("edwdef a fruit that grows on trees");
            cc.CallCommand("edwcls neuter");
            Word word = central.GetWord("þreitt");
            string PropConcatenated = word.GetForm() + "\n" + word.GetPronunciation()
                + "\n" + word.GetRhyme() + "\n" + word.GetTranslation() + "\n"
                + word.GetPartOfSpeech().GetName() + "\n" + word.GetWordClass().GetName()
                + "\n" + word.GetDefinition();



            // assert
            Assert.AreEqual("þreitt\nþri:ht\neitt\napple\nnoun\nneuter\na fruit that grows on trees", PropConcatenated);
        }
    }

    [TestClass]
    public class DeclensionTestClass
    {
        [TestMethod]
        public void CreateDeclensionTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("adddc nominative");


            // assert
            Assert.IsNotNull(central.GetPoS("noun").GetWordClass("noun").GetDeclension("nominative"));
        }

        [TestMethod]
        public void CreateDeclensionInNewWordClassesTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("adddc nominative");
            cc.CallCommand("addwc wordclass");


            // assert
            Assert.IsNotNull(central.GetPoS("noun").GetWordClass("wordclass").GetDeclension("nominative"));
        }

        [TestMethod]
        public void RemoveDeclensionTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("adddc nominative");
            cc.CallCommand("rmdc nominative");


            // assert
            Assert.IsNull(central.GetPoS("noun").GetWordClass("noun").GetDeclension("nominative"));
        }

        [TestMethod]
        public void RenameDeclensionTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("adddc nominative");
            cc.CallCommand("rnmdc nominative accusative");


            // assert
            Assert.IsNotNull(central.GetPoS("noun").GetWordClass("noun").GetDeclension("accusative"));
            Assert.IsNull(central.GetPoS("noun").GetWordClass("noun").GetDeclension("nominative"));
        }
    }

    [TestClass]
    public class WordFormTestClass
    {
        [TestMethod]
        public void CreateWordFormsWithDeclensionTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("adddc accusative");
            cc.CallCommand("addw gleira gli:ra eira fish noun noun");
            Word word = central.GetWord("gleira");
            WordForm? wf = word.GetWordFormByDeclension("accusative");

            // assert
            Assert.IsNotNull(wf);
        }

        [TestMethod]
        public void CreateWordFormsAfterNewDeclensionTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("addw gleira gli:ra eira fish noun noun");
            cc.CallCommand("adddc accusative");
            Word word = central.GetWord("gleira");
            WordForm? wf = word.GetWordFormByDeclension("accusative");

            // assert
            Assert.IsNotNull(wf);
        }

        [TestMethod]
        public void RemoveWordFormsWithDeclensionTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("adddc accusative");
            cc.CallCommand("addw gleira gli:ra eira fish noun noun");
            cc.CallCommand("rmdc accusative");
            Word word = central.GetWord("gleira");
            WordForm? wf = word.GetWordFormByDeclension("accusative");

            // assert
            Assert.IsNull(wf);
        }

        [TestMethod]
        public void WordFormRegexTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos noun");
            cc.CallCommand("focus pos noun");
            cc.CallCommand("adddc accusative");
            cc.CallCommand("focus wc noun");
            cc.CallCommand("focus dc accusative");
            cc.CallCommand("addtu f a$ .$ u");
            cc.CallCommand("addtu p a$ .$ ü");
            cc.CallCommand("addtu r a$ .$ u");
            cc.CallCommand("addw gleira gli:ra eira fish noun noun");
            Word word = central.GetWord("gleira");
            WordForm wf = word.GetWordFormByDeclension("accusative");

            // assert
            Assert.AreEqual("gleiru", wf.GetForm());
            Assert.AreEqual("gli:rü", wf.GetPronunciation());
            Assert.AreEqual("eiru", wf.GetRhymeGroup().GetID());
        }

        [TestMethod]
        public void RhymeIdentityTest()
        {
            // setup
            CentralStorage central = new CentralStorage();
            CommandCentre cc = new CommandCentre(central);

            // act
            cc.CallCommand("addpos verb");
            cc.CallCommand("focus pos verb");
            cc.CallCommand("adddc infinitive");
            cc.CallCommand("addw hógar ho:ghar ógar meet verb verb");
            cc.CallCommand("addw ljógar ljo:ghar ógar meet verb verb");
            cc.CallCommand("addw mjógir mjo:jir ójir shine verb verb");
            Word word1 = central.GetWord("hógar");
            Word word2 = central.GetWord("ljógar");
            Word word3 = central.GetWord("mjógir");
            WordForm? wf1 = word1.GetWordFormByDeclension("infinitive");
            WordForm? wf2 = word2.GetWordFormByDeclension("infinitive");
            WordForm? wf3 = word3.GetWordFormByDeclension("infinitive");

            RhymeGroup? rg1 = central.GetRhymeGroup("ógar");
            RhymeGroup? rg2 = central.GetRhymeGroup("ójir");

            // assert
            Assert.IsNotNull(rg1);
            Assert.IsNotNull(rg2);
            Assert.AreEqual(2, rg1.GetRhymes().Count);
            Assert.AreEqual(1, rg2.GetRhymes().Count);
        }
    }
}