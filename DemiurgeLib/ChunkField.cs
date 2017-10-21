using System.Collections.Generic;
using System.Drawing;

namespace DemiurgeLib
{
    public class ChunkField<T> : IField2d<T>
    {
        public struct Chunk
        {
            public readonly Point MinPoint;
            public readonly IField2d<T> Field;

            public Chunk(int x, int y, IField2d<T> field)
            {
                this.MinPoint = new Point(x, y);
                this.Field = field;
            }

            public bool ContainsPoint(Point pt)
            {
                return pt.X >= this.MinPoint.X && pt.X < this.MinPoint.X + this.Field.Width &&
                    pt.Y >= this.MinPoint.Y && pt.Y < this.MinPoint.Y + this.Field.Height;
            }
        }

        private List<Chunk> chunks;
        private List<Chunk> cachedChunks;
        private readonly int cacheSize;

        public ChunkField(int width, int height, int cacheSize = 3)
        {
            this.Width = width;
            this.Height = height;

            this.chunks = new List<Chunk>();
            this.cachedChunks = new List<Chunk>();
            this.cacheSize = 3;
        }

        public int Width { get; }
        public int Height { get; }

        public T this[int y, int x]
        {
            get
            {
                var chunk = GetChunkForPosition(x, y);
                if (chunk.HasValue)
                {
                    return chunk.Value.Field[y, x];
                }
                else
                {
                    return default(T);
                }
            }
        }

        protected virtual Chunk? GetChunkForPosition(int x, int y)
        {
            return GetChunkForPositionImpl(x, y);
        }

        /// <summary>
        /// Much as I hate this pattern, this is to allow GetChunkForPosition to be
        /// overridden without sacrificing the use cases in TryAddChunk and RemoveChunk.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Chunk? GetChunkForPositionImpl(int x, int y)
        {
            Point pt = new Point(x, y);

            foreach (var chunk in this.cachedChunks)
            {
                if (chunk.ContainsPoint(pt))
                {
                    // Consider making the cache prioritized, so that a cache hit moves that cache element to the top of the list.
                    return chunk;
                }
            }

            foreach (var chunk in this.chunks)
            {
                if (chunk.ContainsPoint(pt))
                {
                    this.cachedChunks.Add(chunk);

                    if (this.cachedChunks.Count > this.cacheSize)
                        this.cachedChunks.RemoveRange(this.cacheSize, this.cachedChunks.Count - this.cacheSize);

                    return chunk;
                }
            }

            return null;
        }

        public bool TryAddChunk(int x, int y, IField2d<T> field)
        {
            if (GetChunkForPositionImpl(x, y).HasValue)
            {
                return false;
            }
            else
            {
                this.chunks.Add(new Chunk(x, y, field));
                return true;
            }
        }

        public void RemoveChunk(int x, int y)
        {
            var chunk = GetChunkForPositionImpl(x, y);
            if (chunk.HasValue)
            {
                this.chunks.Remove(chunk.Value);
                this.cachedChunks.Remove(chunk.Value);
            }
        }

        public void CompressToCache()
        {
            this.chunks.Clear();
            this.chunks.AddRange(this.cachedChunks);
        }
    }
}
