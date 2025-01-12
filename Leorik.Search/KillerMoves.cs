﻿using Leorik.Core;

namespace Leorik.Search
{
    class KillerMoves
    {
        Move[] _moves;
        int _depth;
        int _width;

        public KillerMoves(int width)
        {
            _moves = Array.Empty<Move>();
            _depth = 0;
            _width = width;
        }

        public void Expand(int depth)
        {
            _depth = Math.Max(_depth, depth);
            Array.Resize(ref _moves, _depth * _width);
        }

        public void Add(int ply, Move move)
        {
            int index0 = _width * ply;
            //We shift all moves by one slot to make room but overwrite a potential dublicate of 'move' then store the new 'move' at [0] 
            int last = index0;
            for (; last < index0 + _width - 1; last++)
                if (_moves[last] == move) //if 'move' is present we want to overwrite it instead of the one at [_width-1]
                    break;
            //2. start with last slot and 'save' the previous values until the first slot got dublicated
            for (int index = last; index > index0; index--)
                _moves[index] = _moves[index - 1];
            //3. store new 'move' in the first slot
            _moves[index0] = move;
        }

        public Span<Move> GetSpan(int ply)
        {
            int index0 = _width * ply;
            return new Span<Move>(_moves, index0, _width);
        }

        public bool Contains(int ply, ref Move move)
        {
            int index0 = _width * ply;
            return Array.IndexOf(_moves, move, index0, _width) >= 0;
        }
    }
}
