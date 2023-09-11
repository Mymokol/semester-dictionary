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
        public void CreateDeclensionsTest()
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
    }

    [TestClass]
    public class WordFormTestClass
    {
        
    }
}