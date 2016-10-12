﻿//
// Copyright (c) 2008-2011, Kenneth Bell
//

// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

namespace RC.Framework.FileSystem.SquashFs
{
    using System.IO;

    internal delegate uint AllocateInode();

    internal delegate ushort AllocateId(int id);

    internal delegate uint WriteDataBlock(byte[] buffer, int offset, int count);

    internal delegate uint WriteFragment(int length, out uint offset);

    internal sealed class BuilderContext
    {
        public Stream RawStream { get; set; }

        public int DataBlockSize { get; set; }

        public byte[] IoBuffer { get; set; }

        public AllocateInode AllocateInode { get; set; }

        public AllocateId AllocateId { get; set; }

        public WriteDataBlock WriteDataBlock { get; set; }

        public WriteFragment WriteFragment { get; set; }

        public MetablockWriter InodeWriter { get; set; }

        public MetablockWriter DirectoryWriter { get; set; }
    }
}