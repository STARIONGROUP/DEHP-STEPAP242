using DEHPSTEPAP242.DstController;
using STEP3DAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEHPSTEPAP242.ViewModel
{
    class DstStep3DFileHeaderViewModel:DstBrowserHeaderViewModel

    {

        public STEP3DFile File { set; get; }
        public DstStep3DFileHeaderViewModel(IDstController dstController) : base(dstController) { }

        public  new void UpdateHeader()
        {

            if(File==null || File.HasFailed)

            {
                return;
            }
            STEP3DFile step3d = File;


            FilePath = step3d.FileName;

            var hdr = step3d.HeaderInfo;

            var fdesc = hdr.file_description;
            Description = fdesc.description;
            ImplementationLevel = fdesc.implementation_level;

            var fname = hdr.file_name;
            Name = fname.name;
            TimeStamp = fname.time_stamp;
            Author = fname.author;
            Organization = fname.organization;
            PreprocessorVersion = fname.preprocessor_version;
            OriginatingSystem = fname.originating_system;
            Authorization = fname.authorisation; // Note: STEP AP242 uses british english name

            FileSchema = hdr.file_schema;



        }


    }
}
