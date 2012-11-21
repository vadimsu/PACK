using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ByteArrayScalarTypeConversionLib;
using ChunkChainDataTypes;
using StreamChunckingLib;

namespace ChunkAndChainFileManager
{
    public class ChunkAndChainFileManager
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        public static uint ChunkExists = 0;
        public static uint ChunksCreated = 0;
        public static uint DifferentChains = 0;
        public static uint ChainsCreated = 0;
        public static uint ChainsUpdated = 0;
        public static uint LongerChainsFound = 0;
        public static uint CannotFindChunkInChain = 0;
        public static uint ChainExistInChunkChainsList = 0;
        public static uint ChainAdded2ExistingChunk = 0;
        public static uint CannotGetChain = 0;
        public static uint FoundEqualChains = 0;
        public static uint ChainsReturned = 0;
        public static uint ChunksLoaded = 0;
        public static uint ChainsLoaded = 0;

        //FileManager.FileManager fileManager;
        static Dictionary<long, ChunkCB> chunkMap;
        long chainId4Lookup;
#if WINDOWS_PHONE
        static FileManager.FileManager fileManager = new FileManager.PhoneFileManager("datafiles");
#else
        static FileManager.FileManager fileManager = new FileManager.NonRamFileManager("datafiles");
#endif
        PerformanceMonitoring.PerformanceMonitoring AddUpdateChunkTicks;
        PerformanceMonitoring.PerformanceMonitoring BuildChainAndSaveTicks;
        PerformanceMonitoring.PerformanceMonitoring ChainMatchTicks;
        PerformanceMonitoring.PerformanceMonitoring EncodeTicks;
        PerformanceMonitoring.PerformanceMonitoring GetChainTicks;
        PerformanceMonitoring.PerformanceMonitoring FindChunkInChainTicks;

        public ChunkAndChainFileManager()
        {
            chainId4Lookup = 0;

            AddUpdateChunkTicks = new PerformanceMonitoring.PerformanceMonitoring("AddUpdateChunkTicks");
            BuildChainAndSaveTicks =new PerformanceMonitoring.PerformanceMonitoring("BuildChainAndSaveTicks");
            ChainMatchTicks = new PerformanceMonitoring.PerformanceMonitoring("ChainMatchTicks");
            EncodeTicks = new PerformanceMonitoring.PerformanceMonitoring("EncodeDecodeTicks");
            GetChainTicks = new PerformanceMonitoring.PerformanceMonitoring("GetChainTicks");
            FindChunkInChainTicks = new PerformanceMonitoring.PerformanceMonitoring("FindChunkInChainTicks");
        }
        
        static void GetChainAsByteArray(long[] chunks, out byte[] buffer)
        {
            buffer = new byte[sizeof(long) * chunks.Length + sizeof(uint)];
            uint offset = 0;
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(buffer, offset, (uint)chunks.Length);
            foreach (long l in chunks)
            {
                offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Long2ByteArray(buffer, offset, l);
            }
        }

        static void GetChunkCBAsByteArray(ChunkCB chunkCB,out byte[] buffer)
        {
            buffer = new byte[ChunkCB.GetSize(chunkCB.ChainsListSize)];
            uint offset = 0;
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(buffer, offset, chunkCB.size);
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Long2ByteArray(buffer, offset, chunkCB.sha1);
            buffer[offset++] = chunkCB.hint;
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Long2ByteArray(buffer, offset, chunkCB.fo.file);
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Long2ByteArray(buffer, offset, chunkCB.fo.offset);
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(buffer, offset, chunkCB.ChainsListSize);
            foreach (long l in chunkCB.chains)
            {
                offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Long2ByteArray(buffer, offset, l);
            }
        }

        static bool GetChain(long chainId,out Chain chain)
        {
            Chain chain2 = new Chain();
            byte[] buff;

            if (!fileManager.ReadChainFile(chainId, out buff))
            {
                chain = chain2;
                return false;
            }
            
            uint offset = 0;
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buff, offset, out chain2.NumberOfEntries);
            chain2.chunkIds = new long[chain2.NumberOfEntries];
            for (int idx = 0; idx < chain2.NumberOfEntries; idx++)
            {
                offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Long(buff, offset, out chain2.chunkIds[idx]);
            }
            chain = chain2;
            return true;
        }

        public static int CompareChains(List<long> chunkList, int successorsChunkListIdx, long[] chunkIds, int chunk_idx)
        {
            uint idx = (uint)chunk_idx;
            /* if successors list is longer than found chain, the chain does not match */
            if ((chunkList.Count - successorsChunkListIdx) > (chunkIds.Length - chunk_idx))
            {
                return -1;
            }
            /* for each chunk in successors list check if equals */
            while (successorsChunkListIdx < chunkList.Count)
            {
                if (chunkList[successorsChunkListIdx] != chunkIds[idx])
                {
                    return -1;
                }
                successorsChunkListIdx++;
                idx++;
            }
            return (int)(chunkIds.Length - idx);
        }

        static bool FindChunkInChain(long []chain,long chunk,out int chunk_idx)
        {
            int idx;

            if(chain == null)
            {
                chunk_idx = 0;
                return false;
            }
            chunk_idx = chain.Length;
            idx = 0;
            foreach(long l in chain)
            {
                if(l == chunk)
                {
                    chunk_idx = idx;
                    return true;
                }
                idx++;
            }
            return false;
        }

        static bool FindChainInChunksChainList(long[] chainList, long chainId, out int chain_idx)
        {
            int idx;

            if (chainList == null)
            {
                chain_idx = 0;
                return false;
            }
            chain_idx = chainList.Length;
            idx = 0;
            foreach (long l in chainList)
            {
                if (l == chainId)
                {
                    chain_idx = idx;
                    return true;
                }
                idx++;
            }
            return false;
        }
        static bool ChainIsPresentInChunkCB(ChunkCB chunkCB,long chain4Lookup)
        {
            for (int idx = 0; idx < chunkCB.ChainsListSize; idx++)
            {
                if (chunkCB.chains[idx] == chain4Lookup)
                {
                    return true;
                }
            }
            return false;
        }
        bool ChunkIsPresentInChain(Chain chain, long chunk)
        {
            for (int idx = 0; idx < chain.NumberOfEntries; idx++)
            {
                if (chain.chunkIds[idx] == chunk)
                {
                    return true;
                }
            }
            return false;
        }
        static public bool SaveChain(List<long> chunkList, int chunkListIdx, int LastChunkId, byte[] data, int offset,ChunkAndChainFileManager chunkAndChainFileManager)
        {
            ChunkCB chunkCB;
            byte[] buf;
            bool presentChunks = false;
            int savedChunkListIdx = chunkListIdx;
            int idx;
            Chain chain4Lookup;

            if (chunkAndChainFileManager.chainId4Lookup == 0)
            {
                GetChainAsByteArray(new long[0], out buf);
                chunkAndChainFileManager.chainId4Lookup = fileManager.AddUpdateChainFile(buf, chunkAndChainFileManager.chainId4Lookup);
//                LogUtility.LogUtility.LogFile("****created new chain " + Convert.ToString(chainId4Lookup), ModuleLogLevel);
                ChainsCreated++;
            }
            else
            {
//                LogUtility.LogUtility.LogFile("****updating chain " + Convert.ToString(chainId4Lookup), ModuleLogLevel);
                ChainsUpdated++;
            }

            for (; chunkListIdx <= LastChunkId; chunkListIdx++)
            {
#if false
                if (chunkMap.TryGetValue(chunkId, out chunkCB))
#else
                if (!GetChunkCB(chunkList[chunkListIdx], out chunkCB))
#endif
                {
                    chunkCB = new ChunkCB();
                    chunkCB.sha1 = PackChunking.chunkToSha1(chunkList[chunkListIdx]);
                    chunkCB.size = (uint)PackChunking.chunkToLen(chunkList[chunkListIdx]);
                    fileManager.AddNewChunk(data, (uint)offset, chunkCB.size, chunkList[chunkListIdx], out chunkCB.fo);
#if false
                    {
                        byte[] buff2log = new byte[chunkCB.size];
                        for (int i = 0; i < chunkCB.size; i++)
                        {
                            buff2log[i] = data[i + offset];
                        }
                        LogUtility.LogUtility.LogBinary("_stored", buff2log);
                    }
#endif
                    chunkCB.hint = PackChunking.GetChunkHint(data, (uint)offset, chunkCB.size);
                    chunkCB.chains = new long[1];
                    chunkCB.chains[0] = chunkAndChainFileManager.chainId4Lookup;
                    chunkCB.ChainsListSize = 1;
//                    LogUtility.LogUtility.LogFile("****creating new chunk CB " + Convert.ToString(chunkCB.size) + " " + Convert.ToString(chunkList[chunkListIdx]) + " len " + Convert.ToString(PackChunking.chunkToLen(chunkList[chunkListIdx])) + " hint " + Convert.ToString(chunkCB.hint) + " sha1 " + Convert.ToString(chunkCB.sha1), ModuleLogLevel);
                    ChunksCreated++;
                }
                else if (!ChainIsPresentInChunkCB(chunkCB, chunkAndChainFileManager.chainId4Lookup))
                {
                    presentChunks = true;
                    long[] newChainList = new long[chunkCB.chains.Length + 1];
                    chunkCB.chains.CopyTo(newChainList, 0);
                    newChainList[chunkCB.chains.Length] = chunkAndChainFileManager.chainId4Lookup;
                    chunkCB.chains = newChainList;
                    chunkCB.ChainsListSize++;
//                    LogUtility.LogUtility.LogFile("****updating existing chunk CB " + Convert.ToString(chunkCB.size) + " " + Convert.ToString(chunkList[chunkListIdx]) + " hint " + Convert.ToString(chunkCB.hint) + " sha1 " + Convert.ToString(chunkCB.sha1), ModuleLogLevel);
                    ChainAdded2ExistingChunk++;
                }
                else
                {
//                    LogUtility.LogUtility.LogFile("****chain exists in chunk CB " + Convert.ToString(chunkList[chunkListIdx]) + " " + Convert.ToString(PackChunking.chunkToLen(chunkList[chunkListIdx])) + " chain " + Convert.ToString(chainId4Lookup) + " hint " + Convert.ToString(chunkCB.hint) + " sha1 " + Convert.ToString(chunkCB.sha1), ModuleLogLevel);
                    offset += (int)chunkCB.size;
                    ChainExistInChunkChainsList++;
                    continue;
                }
                offset += (int)chunkCB.size;
                GetChunkCBAsByteArray(chunkCB, out buf);
                fileManager.AddUpdateChunkCtrlFile(chunkList[chunkListIdx], buf, 0, (uint)buf.Length);
            }
            if (!GetChain(chunkAndChainFileManager.chainId4Lookup, out chain4Lookup))
            {
                LogUtility.LogUtility.LogFile("****cannot get chain!!! " + Convert.ToString(chunkAndChainFileManager.chainId4Lookup), ModuleLogLevel);
                CannotGetChain++;
                return presentChunks;
            }
//            LogUtility.LogUtility.LogFile("prepare chain, now " + Convert.ToString(chain4Lookup.NumberOfEntries) + " ( " + Convert.ToString(chain4Lookup.chunkIds.Length) + ")" + " will be added " + Convert.ToString(chunkList.Count-savedChunkListIdx), ModuleLogLevel);
            long[] newChunkList = new long[chain4Lookup.NumberOfEntries + ((LastChunkId+1) - savedChunkListIdx)];
            chain4Lookup.chunkIds.CopyTo(newChunkList, 0);
            idx = (int)chain4Lookup.NumberOfEntries;
            for (chunkListIdx = savedChunkListIdx; chunkListIdx <= LastChunkId; chunkListIdx++)
            {
#if false
                if (ChunkIsPresentInChain(chain4Lookup, chunkList[chunkListIdx]))
                {
                    LogUtility.LogUtility.LogFile("SANITY CHECK: chunk " + Convert.ToString(chunkList[chunkListIdx]) + " len " + Convert.ToString(PackChunking.chunkToLen(chunkList[chunkListIdx])) + " is already in chain", ModuleLogLevel);
                    return presentChunks;
                }
#endif
                newChunkList[idx++] = chunkList[chunkListIdx];
            }
            chain4Lookup.chunkIds = newChunkList;
            chain4Lookup.NumberOfEntries = (uint)newChunkList.Length;
            GetChainAsByteArray(chain4Lookup.chunkIds, out buf);
            //save on disk
            chunkAndChainFileManager.chainId4Lookup = fileManager.AddUpdateChainFile(buf, chunkAndChainFileManager.chainId4Lookup);
            return presentChunks;
        }
        public int ChainMatch(List<long> chunkList,int chunkListIdx,List<ChunkListAndChainId> chainsChunksList)
        {
            Chain chain;
            uint longest_chain = 0;
            int foundChainLength;
            ChunkCB chunkCB;
            ChunkListAndChainId chunkListAndChainId;
            bool AtLeastEqualChainFound = false;

            ChainMatchTicks.EnterFunction();
#if false
            if (chunkMap.TryGetValue(chunkId, out chunkCB))
#else
            if (!GetChunkCB(chunkList[chunkListIdx], out chunkCB))
#endif
            {
                GetChainTicks.LeaveFunction();
//                LogUtility.LogUtility.LogFile("Cannot get chunk " + Convert.ToString(chunkList[chunkListIdx]), LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                return -1;
            }
//            LogUtility.LogUtility.LogFile("****chunk to math found " + Convert.ToString(PackChunking.chunkToLen(chunkList[chunkListIdx])), ModuleLogLevel);
            foreach (long ch in chunkCB.chains)
            {
                GetChainTicks.EnterFunction();
                if (!GetChain(ch, out chain))
                {
                    GetChainTicks.LeaveFunction();
                    LogUtility.LogUtility.LogFile("Cannot get chain!!!! " + Convert.ToString(ch), ModuleLogLevel);
                    CannotGetChain++;
                    continue;
                }
                //LogUtility.LogUtility.LogFile("chain " + Convert.ToString(ch) + " chunks " + Convert.ToString(chain.chunkIds.Length), ModuleLogLevel);
                GetChainTicks.LeaveFunction();
                int chunk_idx;
                FindChunkInChainTicks.EnterFunction();
                if (!FindChunkInChain(chain.chunkIds, chunkList[chunkListIdx], out chunk_idx))
                {
                    FindChunkInChainTicks.LeaveFunction();
//                    LogUtility.LogUtility.LogFile("Cannot find chunk!!!! " + Convert.ToString(chunkList[chunkListIdx]) + " in chain " + Convert.ToString(ch), ModuleLogLevel);
                    CannotFindChunkInChain++;
                    continue;
                }
                FindChunkInChainTicks.LeaveFunction();
                //LogUtility.LogUtility.LogFile("chunk id in chain " + Convert.ToString(chunk_idx), ModuleLogLevel);
                foundChainLength = CompareChains(chunkList, chunkListIdx+1, chain.chunkIds, chunk_idx + 1);
                switch (foundChainLength)
                {
                    case -1: /* different */
                        chainId4Lookup = 0;
                        DifferentChains++;
                        break;
                    case 0:/* match but no longer */
                        //LogUtility.LogUtility.LogFile("equal chain found " + Convert.ToString(longest_chain) , ModuleLogLevel);
                        FoundEqualChains++;
                        AtLeastEqualChainFound = true;
                        if (chainId4Lookup == 0)
                        {
//                            LogUtility.LogUtility.LogFile("equal chain found " + Convert.ToString(longest_chain), ModuleLogLevel);
                            chainId4Lookup = ch;
                        }
                        break;
                    default:
                        {
                            LongerChainsFound++;
                            if (foundChainLength > longest_chain)
                            {
                                longest_chain = (uint)foundChainLength;
                                chainId4Lookup = ch;
                            }
                            AtLeastEqualChainFound = true;
//                            LogUtility.LogUtility.LogFile("****longer chain found " + Convert.ToString(longest_chain), ModuleLogLevel);
                            bool chainFound = false;
                            foreach (ChunkListAndChainId c in chainsChunksList)
                            {
                                if (c.chainId == ch)
                                {
                                    //LogUtility.LogUtility.LogFile("****already in list", ModuleLogLevel);
                                    chainFound = true;
                                }
                            }
                            if (!chainFound)
                            {
                                chunkListAndChainId = new ChunkListAndChainId();
                                chunkListAndChainId.chainId = ch;
                                chunkListAndChainId.chunks = chain.chunkIds;
                                chunkListAndChainId.firstChunkIdx = (uint)chunk_idx + (uint)(chunkList.Count - chunkListIdx);
                                chainsChunksList.Add(chunkListAndChainId);
//                                LogUtility.LogUtility.LogFile("****added to list, first chunk " + Convert.ToString(chunkListAndChainId.firstChunkIdx), ModuleLogLevel);
                            }
                        }
                        break;
                }
            }
            if (!AtLeastEqualChainFound)
            {
                return -1;
            }
            //chainId4Lookup = 0;
            return (int)longest_chain;
        }
        static public byte GetChunkHint(long chunk)
        {
            ChunkCB chunkCB;
#if false
            if (chunkMap.TryGetValue(chunk, out chunkCB))
            {
                return chunkCB.hint;
            }
#else
            if (GetChunkCB(chunk, out chunkCB))
            {
                return chunkCB.hint;
            }
#endif
            return 0;
        }

        static public byte[] GetChunkData(long chunkId)
        {
            ChunkCB chunkCB;
#if false
            if (!chunkMap.TryGetValue(chunkId, out chunkCB))
            {
                return null;
            }
#else
            if (!GetChunkCB(chunkId, out chunkCB))
            {
                return null;
            }
#endif
            return fileManager.GetChunk(chunkCB.fo, chunkCB.size);
        }

        public void OnDispose()
        {
            AddUpdateChunkTicks.Log();
            BuildChainAndSaveTicks.Log();
            ChainMatchTicks.Log();
            GetChainTicks.Log();
            FindChunkInChainTicks.Log();
        }

        static uint GetChunkCB(byte[] buffer, uint offset, out ChunkCB chunkCB)
        {
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, offset, out chunkCB.size);
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Long(buffer, offset, out chunkCB.sha1);
            chunkCB.hint = buffer[offset++];
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Long(buffer, offset, out chunkCB.fo.file);
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Long(buffer, offset, out chunkCB.fo.offset);
            offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, offset, out chunkCB.ChainsListSize);
            chunkCB.chains = new long[chunkCB.ChainsListSize];
            return offset;  
        }

        static uint GetChunkCBAndChainList(byte[] buffer, uint offset, out ChunkCB chunkCB)
        {
            offset = GetChunkCB(buffer, offset, out chunkCB);
            long []chains = new long[chunkCB.ChainsListSize];

            for (int idx = 0; idx < chunkCB.ChainsListSize; idx++)
            {
                offset += ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Long(buffer, offset, out chains[idx]);
                ChainsLoaded++;
            }
            chunkCB.chains = chains;
            return offset;
        }

        static bool GetChunkCB(long chunk,out ChunkCB chunkCB)
        {
            //string filename = "datafiles" + "\\"+ FileManager.FileManager.LongName2StringName(chunk, (byte)FileManager.FileTypes.CHUNK_CTRL_FILE_TYPE);
            try
            {
                byte[] buff = fileManager.ReadFile(chunk);
                if (buff == null)
                {
                    chunkCB = new ChunkCB();
                    return false;
                }
                if (GetChunkCBAndChainList(buff, 0, out chunkCB) == buff.Length)
                {
                    ChunkExists++;
                    return true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            chunkCB = new ChunkCB();
            return false;
        }

        public static void Flush()
        {
            FileManager.NonRamFileManager.OnExiting();
        }
     
        static public void Init()
        {
            //fileManager = new FileManager.FileManager("F:\\datafiles");
            LogUtility.LogUtility.LogFile("ChunkAndChainFileManager:Init", LogUtility.LogLevels.LEVEL_LOG_HIGH);
        }
        public string GetDebugInfo()
        {
            return "ChunksCreated " + Convert.ToString(ChunksCreated) + " ChunkExists " + Convert.ToString(ChunkExists) + " ChainsCreated " + Convert.ToString(ChainsCreated) + " ChainsUpdated " + Convert.ToString(ChainsUpdated) + " FoundEqualChains " + Convert.ToString(FoundEqualChains) + " DifferentChains " + Convert.ToString(DifferentChains) + " LongerChainsFound " + Convert.ToString(LongerChainsFound) + " CannotFindChunkInChain " + Convert.ToString(CannotFindChunkInChain) + " ChainExistInChunkChainsList " + Convert.ToString(ChainExistInChunkChainsList) + " ChainAdded2ExistingChunk " + Convert.ToString(ChainAdded2ExistingChunk) + " CannotGetChain " + Convert.ToString(CannotGetChain) + " ChainsReturned " + Convert.ToString(ChainsReturned);
        }
    }
}
