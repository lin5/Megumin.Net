﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MMONET
{
    /// <summary>
    /// 
    /// </summary>
    public class BufferPool
    {
        /// <summary>
        /// 内存池总大小(统计使用，不保证精确)
        /// </summary>
        public static long totalBufferSize = 0;

        // 16 32 64 128 256 512 1024 2048 4096 8192
        static readonly Pool[] pools = new Pool[10];

        /// <summary>
        /// 二分查找
        /// </summary>
        /// <param name="needSize"></param>
        /// <returns></returns>
        static Pool GetPool(int needSize)
        {
            if (needSize == 256)
            {
                return pools[4];
            }
            else if (needSize < 256)
            {
                if (needSize == 64)
                {
                    return pools[2];
                }
                else if (needSize < 64)
                {
                    if (needSize <= 16)
                    {
                        return pools[0];
                    }
                    else
                    {
                        return pools[1];
                    }
                }
                else
                {
                    if (needSize <= 128)
                    {
                        return pools[3];
                    }
                    else
                    {
                        return pools[4];
                    }
                }
            }
            else
            {
                if (needSize == 2048)
                {
                    return pools[7];
                }
                else if (needSize < 2048)
                {
                    if (needSize <= 512)
                    {
                        return pools[5];
                    }
                    else
                    {
                        if (needSize <= 1024)
                        {
                            return pools[6];
                        }
                        else
                        {
                            return pools[7];
                        }
                    }
                }
                else
                {
                    if (needSize <= 4096)
                    {
                        return pools[8];
                    }
                    else
                    {
                        return pools[9];
                    }
                }
            }
        }

        static BufferPool()
        {
            for (int i = 0; i < 10; i++)
            {
                pools[i] = new Pool((int)Math.Pow(2,i + 4),(10 - i) * 100);
            }
        }


        /// <summary>
        /// 取得buffer,保证buffer长度大于参数长度,最大8192
        /// </summary>
        /// <param name="needLength"></param>
        /// <returns></returns>
        public static byte[] Pop(int needLength)
        {
            return GetPool(needLength).Pop();
        }

        /// <summary>
        /// 归还buffer
        /// </summary>
        /// <param name="buffer"></param>
        public static void Push(byte[] buffer)
        {
            GetPool(buffer.Length).Push(buffer);
        }

        /// <summary>
        /// 容量是服务器内存逻辑核数的2倍
        /// </summary>
        static readonly Pool pool65536 = new Pool(65536,64);
        /// <summary>
        /// 取得长度为65536的buffer
        /// </summary>
        public static byte[] Pop65536()
        {
            return pool65536.Pop();
        }

        /// <summary>
        /// 归还长度为65536的buffer
        /// </summary>
        /// <param name="buffer"></param>
        public static void Push65536(byte[] buffer)
        {
            if (buffer.Length != 65536)
            {
                return;
            }

            pool65536.Push(buffer);
        }

        

        class Pool
        {
            public Pool(int Size,int maxCacheCount = 97)
            {
                this.Size = Size;
                this.MaxCacheCount = maxCacheCount;
                bufferPoop = new ConcurrentQueue<byte[]>();
                for (int i = 0; i < MaxCacheCount / 10; i++)
                {
                    bufferPoop.Enqueue(Create());
                }
            }

            public int Size { get; }
            /// <summary>
            /// 池中最大保留实例个数
            /// </summary>
            public int MaxCacheCount { get; set; }

            readonly ConcurrentQueue<byte[]> bufferPoop;
            internal byte[] Pop()
            {
                if (bufferPoop.TryDequeue(out var buffer))
                {
                    return buffer;
                }

                //if (bufferPoop.Count > 0)
                //{
                //    var buffer = bufferPoop.Dequeue();
                //    if (buffer == null)
                //    {
                //        return Create();
                //    }

                //    return buffer;
                //}
                else
                {
                    return Create();
                }
            }

            byte[] Create()
            {
                Interlocked.Add(ref totalBufferSize, Size);
                return new byte[Size];
            }

            internal void Push(byte[] buffer)
            {
                if (bufferPoop.Count >= MaxCacheCount/2)
                {
                    ///舍弃 buffer
                    ///如果buffer 不是 BufferPool内new 出来的，TotalBufferSize 将不精确
                    totalBufferSize -= Size;
                }
                else
                {
                    bufferPoop.Enqueue(buffer);
                }
            }
        }
    }
}
