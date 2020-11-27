#include <CppUnitTest.h>
using namespace Microsoft::VisualStudio::CppUnitTestFramework;


#include "step3d_wrapper.h"

// Valid for /std:c++17
#include <filesystem>
namespace fs = std::filesystem;


// To test the auxiliary feature of creating an image from the 
// IStep3D_Wrapper's information it is required to have the
// GraphViz application installed.
//
//#define ENABLE_DOT_GRAPH_GENERATION


namespace Step3D_Wrapper_Tests
{
    // Paths to example files
    std::filesystem::path MyParts_path;
    std::filesystem::path NotStep3DFile_path;

    TEST_MODULE_INITIALIZE(IStep3D_Wrapper_Test)
    {
        Logger::WriteMessage("Initialize Module: file paths...\n");

        // Compose paths
        // Expected CWD: /path/to/STEP3DWrapper/builds/<build_name>/bin
        // Examples at:  /path/to/STEP3DWrapper/STEPcode/extra/step3d_wrapper_test/examples

        std::filesystem::path cwd = fs::current_path();
        MyParts_path = std::filesystem::absolute(cwd / "../../../STEPcode/extra/step3d_wrapper_test/examples/MyParts.step");
        NotStep3DFile_path = std::filesystem::absolute(cwd / "../../../STEPcode/extra/step3d_wrapper_test/examples/NotStepFileFormat.step");

        // Show composed paths
        Logger::WriteMessage(std::string("MyParts.step: ").append(MyParts_path.string()).append("\n").c_str());
        Logger::WriteMessage(std::string("NotStepFileFormat.step: ").append(NotStep3DFile_path.string()).append("\n").c_str());
    }

    /*
    * @brief Unit tests for IStep3d_Wrapper included in the step3d_wrapper.dll
    */
    TEST_CLASS(IStep3D_Wrapper_Tests)
    {
    public:

        IStep3D_Wrapper_Tests()
        {
        }

        ~IStep3D_Wrapper_Tests()
        {
            Logger::WriteMessage("In ~IStep3D_Wrapper_Tests\n");
        }

        TEST_METHOD(IStep3D_Wrapper_CheckVersion_PrefixMatch)
        {
            std::string expected = "git commit id: 0.8";
            std::string sc_version = getStepcodeVersion();

            Assert::AreEqual(expected, sc_version.substr(0, expected.size()));
        }

        TEST_METHOD(IStep3D_Wrapper_NewRelease_CreatedDestroyed)
        {
            Logger::WriteMessage("Create and Release IStep3D_Wrapper instance\n");

            IStep3D_Wrapper* wrapper = CreateIStep3D_Wrapper();

            Assert::IsNotNull(wrapper);

            wrapper->Release();
        }

        TEST_METHOD(IStep3D_Wrapper_LoadNotExistingFile_NotLoaded)
        {
            IStep3D_Wrapper* wrapper = CreateIStep3D_Wrapper();

            Logger::WriteMessage("Loading non-existing file\n");

            Assert::IsFalse(wrapper->load("not-file-found.step"));

            Assert::AreEqual("File does not exists: not-file-found.step", wrapper->getErrorMessage().c_str());

            wrapper->Release();
        }

        TEST_METHOD(IStep3D_Wrapper_LoadBadFormatFile_NotLoaded)
        {
            IStep3D_Wrapper* wrapper = CreateIStep3D_Wrapper();

            Assert::IsFalse(wrapper->load(NotStep3DFile_path.string()));

            Assert::AreEqual("Error reading the STEP file content: SEVERITY_INPUT_ERROR", wrapper->getErrorMessage().c_str());

            wrapper->Release();
        }

        TEST_METHOD(IStep3D_Wrapper_LoadExistingFile_Loaded)
        {
            IStep3D_Wrapper* wrapper = CreateIStep3D_Wrapper();

            Assert::IsTrue(wrapper->load(MyParts_path.string()));

            wrapper->Release();
        }

        TEST_METHOD(IStep3D_Wrapper_ReadContentFromUnloadedFile_shouldFail)
        {
            IStep3D_Wrapper* wrapper = CreateIStep3D_Wrapper();

            Assert::IsFalse(wrapper->parseHLRInformation());
            //auto func = [&wrapper] { wrapper->parseHLRInformation(); };
            //Assert::ExpectException<std::exception>(func);

            Assert::AreEqual("No loaded file yet, parse content is not possible", wrapper->getErrorMessage().c_str());
            
            wrapper->Release();
        }

        TEST_METHOD(IStep3D_Wrapper_MyPartsContent_isOK)
        {
            IStep3D_Wrapper* wrapper = CreateIStep3D_Wrapper();

            Assert::IsTrue(wrapper->load(MyParts_path.string()));
            Assert::IsTrue(wrapper->parseHLRInformation());
            Assert::IsFalse(wrapper->hasFailed());

            // Check retrieved information

            auto hdr = wrapper->getHeaderInfo();
            Assert::AreEqual("('FreeCAD Model')", hdr.file_description.description.c_str());
            Assert::AreEqual("'2;1'", hdr.file_description.implementation_level.c_str());
            Assert::AreEqual("'D:/dev/DEHP/SharePoint/Project \nDocuments/XIPE_STEP_3D_Samples/MyParts.step'", hdr.file_name.name.c_str());
            Assert::AreEqual("'2020-09-01T18:50:05'", hdr.file_name.time_stamp.c_str());
            Assert::AreEqual("('Author')", hdr.file_name.author.c_str());
            Assert::AreEqual("('')", hdr.file_name.organization.c_str());
            Assert::AreEqual("'Open CASCADE STEP processor 7.2'", hdr.file_name.preprocessor_version.c_str());
            Assert::AreEqual("'FreeCAD'", hdr.file_name.originating_system.c_str());
            Assert::AreEqual("'Unknown'", hdr.file_name.authorisation.c_str());
            Assert::AreEqual("('AUTOMOTIVE_DESIGN { 1 0 10303 214 1 1 1 1 }')", hdr.file_schema.c_str());

            auto nodes = wrapper->getNodes();
            Assert::AreEqual((size_t)5, nodes.size());

            auto itNode = nodes.begin();
            Assert::AreEqual(5, itNode->id);
            Assert::AreEqual("'Part'", itNode->name.c_str());
            Assert::AreEqual("Shape_Representation", itNode->representation_type.c_str());

            itNode++;
            Assert::AreEqual(367, itNode->id);
            Assert::AreEqual("'Caja'", itNode->name.c_str());
            Assert::AreEqual("Advanced_Brep_Shape_Representation", itNode->representation_type.c_str());

            itNode++;
            Assert::AreEqual(380, itNode->id);
            Assert::AreEqual("'SubPart'", itNode->name.c_str());
            Assert::AreEqual("Shape_Representation", itNode->representation_type.c_str());

            itNode++;
            Assert::AreEqual(737, itNode->id);
            Assert::AreEqual("'Cube'", itNode->name.c_str());
            Assert::AreEqual("Advanced_Brep_Shape_Representation", itNode->representation_type.c_str());

            itNode++;
            Assert::AreEqual(854, itNode->id);
            Assert::AreEqual("'Cylinder'", itNode->name.c_str());
            Assert::AreEqual("Advanced_Brep_Shape_Representation", itNode->representation_type.c_str());

            auto relations = wrapper->getRelations();
            Assert::AreEqual((size_t)4, relations.size());

            auto itRel = relations.begin();
            Assert::AreEqual(376, itRel->id);
            Assert::AreEqual("'=>[0:1:1:1]'", itRel->name.c_str());
            Assert::AreEqual("NUAO", itRel->type.c_str());
            Assert::AreEqual(5, itRel->relating_id);
            Assert::AreEqual(367, itRel->related_id);

            itRel++;
            Assert::AreEqual(746, itRel->id);
            Assert::AreEqual("'=>[0:1:1:2]'", itRel->name.c_str());
            Assert::AreEqual("NUAO", itRel->type.c_str());
            Assert::AreEqual(380, itRel->relating_id);
            Assert::AreEqual(737, itRel->related_id);

            itRel++;
            Assert::AreEqual(863, itRel->id);
            Assert::AreEqual("'=>[0:1:1:3]'", itRel->name.c_str());
            Assert::AreEqual("NUAO", itRel->type.c_str());
            Assert::AreEqual(380, itRel->relating_id);
            Assert::AreEqual(854, itRel->related_id);

            itRel++;
            Assert::AreEqual(869, itRel->id);
            Assert::AreEqual("'=>[0:1:1:4]'", itRel->name.c_str());
            Assert::AreEqual("NUAO", itRel->type.c_str());
            Assert::AreEqual(5, itRel->relating_id);
            Assert::AreEqual(380, itRel->related_id);

            wrapper->Release();
        }
    };


#ifdef ENABLE_DOT_GRAPH_GENERATION

    /*
    * @brief Unit tests for ICreateITreeGraphGenerator_Wrapper included in the step3d_wrapper.dll
    */
    TEST_CLASS(ICreateITreeGraphGenerator_Wrapper_Tests)
    {
    public:

        ICreateITreeGraphGenerator_Wrapper_Tests()
        {
        }

        ~ICreateITreeGraphGenerator_Wrapper_Tests()
        {
        }

        TEST_METHOD(ICreateITreeGraphGenerator_Wrapper_ExportMyPartsGraphs_Generated)
        {
            IStep3D_Wrapper* wrapper = CreateIStep3D_Wrapper();
            ITreeGraphGenerator_Wrapper* graphGenerator = CreateITreeGraphGenerator_Wrapper();
            
            Assert::IsTrue(wrapper->load(MyParts_path.string()));
            Assert::IsTrue(wrapper->parseHLRInformation());
            Assert::IsTrue(graphGenerator->generate(wrapper, TreeGraphStyle::All_Graphs));

            graphGenerator->Release();
            wrapper->Release();
        }
    };

#endif // ENABLE_DOT_GRAPH_GENERATION

}
