/**
 * Copyright 2011 Eyal Zohar. All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
 * following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following
 * disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the
 * following disclaimer in the documentation and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY EYAL ZOHAR ''AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
 * TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
 * EYAL ZOHAR OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
 * TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 * 
 * The views and conclusions contained in the software and documentation are those of the authors and should not be
 * interpreted as representing official policies, either expressed or implied, of Eyal Zohar.
 */
using System;
using System.IO;
using System.Collections.Generic;
//import java.util.HashSet;
//import java.util.List;

/**
 * Global list of chunks and the files where they appear.
 * 
 * @author Eyal Zohar
 * 
 */
namespace StreamChunkingLib
{
    public class ChunksFiles
{
	private class FileAndOffset
	{
		public File file;
		public long	offset;

		public FileAndOffset(File file, long offset)
		{
			this.file = file;
			this.offset = offset;
		}
	};

	/**
	 * Key is a chunk and value is a file list that have this chunk.
	 */
	private Dictionary<long, HashSet<FileAndOffset>>	chunksFiles	= new Dictionary<long, HashSet<FileAndOffset>>();

	/**
	 * 
	 * @param file
	 *            File object.
	 * @param chunkList
	 *            Chunk list, maybe with duplicates.
	 * @return Number of new chunks, that were not in the list before. Duplicates within the file are considered only
	 *         once.
	 */
    public int addFile(File file, List<long> chunkList)
    {
        // Sanity check
        if (file == null || chunkList == null || (chunkList.Count == 0))
            return 0;

        int result = 0;
        long offset = 0;

        // Walk through the chunks
        foreach (long curChunk in chunkList)
        {
            HashSet<FileAndOffset> files;
            files = null;
            chunksFiles.TryGetValue(curChunk, out files);

            if (files == null)
            {
                // Create a new file list for the current chunk
                files = new HashSet<FileAndOffset>();
                chunksFiles.Add(curChunk, files);
                // Indicate that the current chunk is new to this list
                result++;
            }

            // Add the file to the file list for the current chunk
            files.Add(new FileAndOffset(file, offset));

            offset += StreamChunckingLib.PackChunking.chunkToLen(curChunk);
        }

        return result;
    }

	public void printOverlaps(List<long> chunkList, int maxChunksToPrint)
	{
		if (chunkList == null)
			return;

		Console.WriteLine("    serial  hash         size    offset1   offset2   file2");
		Console.WriteLine("    ------- ------------ ------- --------- --------- -------------------");

		int offset = 0;
		int chunkSerial = 1;
		foreach(long curChunk in chunkList)
		{
			HashSet<FileAndOffset> files = null;
            chunksFiles.TryGetValue(curChunk, out files);
			if (files != null)
			{
				if (maxChunksToPrint <= 0)
				{
					Console.WriteLine("   ...");
					return;
				}

				foreach(FileAndOffset curFileAndOffset in files)
				{
                    Console.WriteLine(String.Format("    %,7d %s %,9d %,9d %s", chunkSerial, StreamChunckingLib.PackChunking
									.chunkToString(curChunk), offset, curFileAndOffset.offset, curFileAndOffset.file));
				}

				maxChunksToPrint--;
			}

            offset += StreamChunckingLib.PackChunking.chunkToLen(curChunk);
			chunkSerial++;
		}
	}

	/**
	 * Get the number of bytes in a given chunk list that overlaps the global list.
	 * 
	 * @param chunkList
	 *            Chunk list.
	 * @return Number of bytes in a given chunk list that overlaps the global list.
	 */
	public long getOverlapsSize(List<long> chunkList)
	{
		if (chunkList == null)
			return 0;

		long result = 0;

		foreach(long curChunk in chunkList)
		{
			if (chunksFiles.ContainsKey(curChunk))
                result += StreamChunckingLib.PackChunking.chunkToLen(curChunk);
		}

		return result;
	}
  }
}
