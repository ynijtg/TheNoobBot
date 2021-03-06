﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using meshReader.Helper;

namespace meshReader.Game.WMO
{
    public class WorldModelRoot
    {
        public string Path { get; private set; }
        public ChunkedData Data { get; private set; }
        public WorldModelHeader Header { get; private set; }
        public List<DoodadInstance> DoodadInstances { get; private set; }
        public List<DoodadSet> DoodadSets { get; private set; }
        public List<WorldModelGroup> Groups { get; private set; }
        public List<WorldModelMaterialTexture> Materials { get; private set; }

        public WorldModelRoot(string path)
        {
            uint fileId;
            uint.TryParse(path, out fileId);
            Data = fileId > 0 ? new ChunkedData(fileId) : new ChunkedData(path);
            Path = path;

            ReadHeader();
            //ReadMaterialTextures();
            ReadGroups();
            ReadDoodadInstances();
            ReadDoodadSets();
        }

        private void ReadGroups()
        {
            string pathBase;
            uint fileId;
            uint.TryParse(Path, out fileId);
            if (fileId <= 0)
            {
                pathBase = Path.Substring(0, Path.LastIndexOf('.'));
                Groups = new List<WorldModelGroup>((int) Header.CountGroups);
                for (int i = 0; i < Header.CountGroups; i++)
                {
                    try
                    {
                        Groups.Add(new WorldModelGroup(string.Format("{0}_{1:000}.wmo", pathBase, i), i));
                    }
                    catch (FileNotFoundException)
                    {
                        // ignore missing groups
                    }
                }
            }
            else
            {
                var chunk = Data.GetChunkByName("GFID");
                if (chunk == null)
                    return;
                var stream = chunk.GetStream();
                var r = new BinaryReader(stream);
                Groups = new List<WorldModelGroup>((int) Header.CountGroups);
                for (int i = 0; i < Header.CountGroups; i++)
                {
                    try
                    {
                        uint fileIdGroup = r.ReadUInt32();
                        Groups.Add(new WorldModelGroup(fileIdGroup.ToString(), i));
                    }
                    catch (FileNotFoundException)
                    {
                        // ignore missing groups
                    }
                }
            }
        }

        private void ReadDoodadSets()
        {
            var chunk = Data.GetChunkByName("MODS");
            if (chunk == null)
                return;

            var stream = chunk.GetStream();
            Debug.Assert(chunk.Length / 32 == Header.CountSets);
            DoodadSets = new List<DoodadSet>((int) Header.CountSets);
            for (int i = 0; i < Header.CountSets; i++)
                DoodadSets.Add(DoodadSet.Read(stream));
        }

        private void ReadDoodadInstances()
        {
            var chunk = Data.GetChunkByName("MODD");
            var nameChunk = Data.GetChunkByName("MODN");
            if (chunk == null || nameChunk == null)
                return;

            const int instanceSize = 40;
            var countInstances = (int) (chunk.Length / instanceSize);
            DoodadInstances = new List<DoodadInstance>(countInstances);
            for (int i = 0; i < countInstances; i++)
            {
                var stream = chunk.GetStream();
                stream.Seek(instanceSize * i, SeekOrigin.Current);
                var instance = DoodadInstance.Read(stream);
                var nameStream = nameChunk.GetStream();
                if (instance.FileOffset >= nameChunk.Length)
                    continue;
                nameStream.Seek(instance.FileOffset, SeekOrigin.Current);
                instance.File = nameStream.ReadCString();
                DoodadInstances.Add(instance);
            }
        }

        private void ReadHeader()
        {
            var chunk = Data.GetChunkByName("MOHD");
            if (chunk == null)
                return;

            var stream = chunk.GetStream();
            Header = WorldModelHeader.Read(stream);
        }

        private void ReadMaterialTextures()
        {
            var chunk = Data.GetChunkByName("MOMT");
            var nameChunk = Data.GetChunkByName("MOTX");
            if (chunk == null || nameChunk == null)
                return;

            const int materialSize = 64;
            var countMaterials = (int) (chunk.Length / materialSize);
            Materials = new List<WorldModelMaterialTexture>(countMaterials);
            for (int i = 0; i < countMaterials; i++)
            {
                var stream = chunk.GetStream();
                stream.Seek(materialSize * i, SeekOrigin.Current);
                var material = WorldModelMaterialTexture.Read(stream);
                var nameStream = nameChunk.GetStream();
                if (material.Texture1 >= nameChunk.Length)
                    continue;
                nameStream.Seek(material.Texture1, SeekOrigin.Current);
                material.Texture1Name = nameStream.ReadCString();
                Materials.Add(material);
            }
        }
    }
}