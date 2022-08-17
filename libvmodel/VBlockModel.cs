using System;
using System.IO;
using System.Collections.Generic;

namespace libvmodel{
    public class VBlockModel{

        VModel model;

        byte blockedFaces;

        public VBlockModel(VModel model, byte blockedFaces){
            this.model = model;
            this.blockedFaces = blockedFaces;
        }

        /**
        <summary>
        loads a VBlockModel from a vmf file.
        The folder and file are given separately so that we can load the resources properly;
        the assets referenced by the vmf file are reletive to the folder.
        The path of the vmf is defined as 'folder + "/" + file'
        
        The vmf parameter is for when you also need data from the vmf file.
        </summary>
        */
        public VBlockModel(string folder, string file, out Dictionary<string, string>? vmf, out ICollection<string>? errors){
            this.model = new VModel(folder, file, out vmf, out errors);
        }

    }
}