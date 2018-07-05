using BeatSaberSongGenerator.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberSongGenerator.Generators
{
    public struct Candidates
    {
        public Candidates(string[] candidates)
        {
            CandidateStrings = candidates;
        }
        public string[] CandidateStrings { get; }

        public Candidates Flipped()
        {
            if (CandidateStrings == null) return new Candidates();
            string[] flipped = new string[CandidateStrings.Length];
            for (int i = 0; i < CandidateStrings.Length; ++i)
            {
                var row1 = CandidateStrings[i].Substring(0, 4).Reverse();
                var row2 = CandidateStrings[i].Substring(4, 4).Reverse();
                var row3 = CandidateStrings[i].Substring(8, 4).Reverse();
                var all = row1.Concat(row2).Concat(row3);
                string str = "";
                foreach (var c in all)
                {
                    str += c;
                }
                flipped[i] = str;
            }
            return new Candidates(flipped);
        }
    }

    class NoteCandidatesStateMachine
    {
        private String noNotes = "0000" + "0000" + "0000";
        private Random rand = new Random();

        public enum NotePosition
        {
            TopLeft,
            TopCenterLeft,
            TopCenterRight,
            TopRight,
            MiddleLeft,
            MiddleCenterLeft,
            MiddleCenterRight,
            MiddleRight,
            BottomLeft,
            BottomCenterLeft,
            BottomCenterRight,
            BottomRight
        }

        public enum NoteDirection
        {
            Up,
            UpRight,
            Right,
            DownRight,
            Down,
            DownLeft,
            Left,
            UpLeft
        }

        public NotePosition ToPositionEnum(HorizontalPosition horizontalPosition, VerticalPosition verticalPosition)
        {
            if (horizontalPosition == HorizontalPosition.CenterLeft && verticalPosition == VerticalPosition.Bottom) return NotePosition.BottomCenterLeft;
            if (horizontalPosition == HorizontalPosition.CenterLeft && verticalPosition == VerticalPosition.Middle) return NotePosition.MiddleCenterLeft;
            if (horizontalPosition == HorizontalPosition.CenterLeft && verticalPosition == VerticalPosition.Top) return NotePosition.TopCenterLeft;
            if (horizontalPosition == HorizontalPosition.CenterRight && verticalPosition == VerticalPosition.Bottom) return NotePosition.BottomCenterRight;
            if (horizontalPosition == HorizontalPosition.CenterRight && verticalPosition == VerticalPosition.Middle) return NotePosition.BottomCenterRight;
            if (horizontalPosition == HorizontalPosition.CenterRight && verticalPosition == VerticalPosition.Top) return NotePosition.BottomCenterRight;
            if (horizontalPosition == HorizontalPosition.Left && verticalPosition == VerticalPosition.Bottom) return NotePosition.BottomLeft;
            if (horizontalPosition == HorizontalPosition.Left && verticalPosition == VerticalPosition.Middle) return NotePosition.MiddleLeft;
            if (horizontalPosition == HorizontalPosition.Left && verticalPosition == VerticalPosition.Top) return NotePosition.TopLeft;
            if (horizontalPosition == HorizontalPosition.Right && verticalPosition == VerticalPosition.Bottom) return NotePosition.BottomRight;
            if (horizontalPosition == HorizontalPosition.Right && verticalPosition == VerticalPosition.Middle) return NotePosition.MiddleRight;
            if (horizontalPosition == HorizontalPosition.Right && verticalPosition == VerticalPosition.Top) return NotePosition.TopRight;
            throw new ArgumentOutOfRangeException();
        }

        public NoteDirection ToDirectionEnum(CutDirection cutDirection)
        {
            switch (cutDirection)
            {
                case CutDirection.Up:
                    return NoteDirection.Up;
                case CutDirection.Down:
                    return NoteDirection.Down;
                case CutDirection.Left:
                    return NoteDirection.Left;
                case CutDirection.Right:
                    return NoteDirection.Right;
                case CutDirection.UpLeft:
                    return NoteDirection.UpLeft;
                case CutDirection.UpRight:
                    return NoteDirection.UpRight;
                case CutDirection.DownLeft:
                    return NoteDirection.DownLeft;
                case CutDirection.DownRight:
                    return NoteDirection.DownRight;
                case CutDirection.Any: //can happen if the last note was any and now we determine the next possible ones
                    switch (rand.Next(4))
                    {
                        case 0: return NoteDirection.Up;
                        case 1: return NoteDirection.Right;
                        case 2: return NoteDirection.Down;
                        case 3: return NoteDirection.Left;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Candidates GetLeftCandidates(NoteDirection direction, NotePosition position)
        {
            switch (direction)
            {
                case NoteDirection.Up:
                    return GetLeftCandidatesForUp(position);
                case NoteDirection.UpRight:
                    return new Candidates();
                case NoteDirection.Right:
                    return GetLeftCandidatesForRight(position);
                case NoteDirection.DownRight:
                    return new Candidates();
                case NoteDirection.Down:
                    return GetLeftCandidatesForDown(position);
                case NoteDirection.DownLeft:
                    return new Candidates();
                case NoteDirection.Left:
                    return GetLeftCandidatesForLeft(position);
                case NoteDirection.UpLeft:
                    return new Candidates();
            }

            return new Candidates();
        }

        private Candidates GetLeftCandidatesForUp(NotePosition position)
        {
            switch (position)
            {
                case NotePosition.TopLeft:
                    return new Candidates(new String[]{
                        noNotes,
                        noNotes,
                        "2000"+
                        "7611"+
                        "3100"
                        ,
                        noNotes,
                        "6731"+
                        "9950"+
                        "7610"
                        ,
                        noNotes,
                        "1000"+
                        "3300"+
                        "4400"
                        ,
                        noNotes
                    });
                case NotePosition.TopCenterLeft:
                    return new Candidates(new String[]{
                        noNotes,
                        noNotes,
                        "0000"+
                        "5400"+
                        "2200",
                        noNotes,
                        "5941"+
                        "5841"+
                        "3531"
                        ,
                        noNotes,
                        "0000"+
                        "4310"+
                        "2100",
                        noNotes
                    });
                case NotePosition.TopCenterRight:
                    return new Candidates(new String[]{
                        noNotes,
                        noNotes,
                        "0000"+
                        "3740"+
                        "2100",
                        noNotes,
                        "7858"+
                        "5999"+
                        "3444",
                        noNotes,
                        "0000"+
                        "5741"+
                        "3531",
                        noNotes
                    });
                case NotePosition.TopRight:
                    return new Candidates(new String[]{
                        noNotes,
                        noNotes,
                        "0000"+
                        "0254"+
                        "0122",
                        noNotes,
                        "3578"+
                        "2499"+
                        "1244",
                        noNotes,
                        "0000"+
                        "4466"+
                        "4456",
                        noNotes
                    });
                case NotePosition.MiddleLeft:
                    return new Candidates(new String[] {
                        noNotes,
                        noNotes,
                        "0090"+
                        "0320"+
                        "6620",
                        noNotes,
                        "3710"+
                        "2731"+
                        "5720",
                        noNotes,
                        "0000"+
                        "0200"+
                        "3300",
                        noNotes
                    });
                case NotePosition.MiddleCenterLeft:
                    return new Candidates(new String[] {
                        noNotes,
                        noNotes,
                        "2000"+
                        "3031"+
                        "5550",
                        noNotes,
                        "4341"+
                        "7973"+
                        "6761",
                        noNotes,
                        "4000"+
                        "3000"+
                        "4410",
                        noNotes
                    });
                case NotePosition.MiddleCenterRight:
                    return new Candidates(new String[] {
                        noNotes,
                        noNotes,
                        "1510"+
                        "3902"+
                        "7731",
                        noNotes,
                        "7898"+
                        "9993"+
                        "3631",
                        noNotes,
                        "5300"+
                        "5401"+
                        "3221",
                        noNotes
                    });
                case NotePosition.MiddleRight:
                    return new Candidates(new String[] {
                        noNotes,
                        noNotes,
                        "0420"+
                        "0740"+
                        "0662",
                        noNotes,
                        "0770"+
                        "1799"+
                        "0344",
                        noNotes,
                        "1510"+
                        "3420"+
                        "0441",
                        noNotes
                    });
                case NotePosition.BottomLeft:
                    return new Candidates(new String[] {
                        noNotes,
                        noNotes,
                        "0241"+
                        "3131"+
                        "0210",
                        noNotes,
                        "3730"+
                        "5520"+
                        "9760",
                        noNotes,
                        "0100"+
                        "0510"+
                        "0310",
                        noNotes
                    });
                case NotePosition.BottomCenterLeft:
                    return new Candidates(new String[] {
                        noNotes,
                        noNotes,
                        "2021"+
                        "7122"+
                        "5251",
                        noNotes,
                        "0210"+
                        "3520"+
                        "8751",
                        noNotes,
                        "0100"+
                        "2310"+
                        "0220",
                        noNotes
                    });
                case NotePosition.BottomCenterRight:
                    return new Candidates(new String[] {
                        noNotes,
                        noNotes,
                        "0211"+
                        "5713"+
                        "4604",
                        noNotes,
                        "1654"+
                        "1955"+
                        "1894",
                        noNotes,
                        "3410"+
                        "3711"+
                        "2201",
                        noNotes,
                    });
                case NotePosition.BottomRight:
                    return new Candidates(new String[] {
                        noNotes,
                        noNotes,
                        "0342"+
                        "2781"+
                        "2780",
                        noNotes,
                        "0673"+
                        "1572"+
                        "2783",
                        noNotes,
                        "3850"+
                        "2971"+
                        "1660",
                        noNotes
                    });
            }
            return new Candidates();
        }

        private Candidates GetLeftCandidatesForRight(NotePosition position)
        {
            switch (position)
            {
                case NotePosition.TopLeft:
                    return new Candidates(new String[]{
                        "0000"+
                        "2240"+
                        "5510",
                        noNotes,
                        noNotes,
                        noNotes,
                        "0243"+
                        "7871"+
                        "7870",
                        noNotes,
                        "9530"+
                        "5630"+
                        "7530",
                        noNotes
                    });
                case NotePosition.TopCenterLeft:
                    return new Candidates(new String[]{
                        "5000"+
                        "9310"+
                        "3200",
                        noNotes,
                        noNotes,
                        noNotes,
                        "9241"+
                        "7872"+
                        "4540",
                        noNotes,
                        "5930"+
                        "5720"+
                        "4640",
                        noNotes
                    });
                case NotePosition.TopCenterRight:
                    return new Candidates(new String[]{
                        "1300"+
                        "4320"+
                        "0240",
                        noNotes,
                        noNotes,
                        noNotes,
                        "5992"+
                        "4991"+
                        "2751",
                        noNotes,
                        "9791"+
                        "3440"+
                        "0230",
                        noNotes
                    });
                case NotePosition.TopRight:
                    return new Candidates(new String[]{
                        "0330"+
                        "0220"+
                        "0000",
                        noNotes,
                        noNotes,
                        noNotes,
                        "2993"+
                        "3992"+
                        "0441",
                        noNotes,
                        "9873"+
                        "5551"+
                        "0331",
                        noNotes
                    });
                case NotePosition.MiddleLeft:
                    return new Candidates(new String[] {
                        "2210"+
                        "1110"+
                        "6630",
                        noNotes,
                        noNotes,
                        noNotes,
                        "9940"+
                        "5200"+
                        "5970",
                        noNotes,
                        "7620"+
                        "9520"+
                        "5310",
                        noNotes
                    });
                case NotePosition.MiddleCenterLeft:
                    return new Candidates(new String[] {
                        "1210"+
                        "1210"+
                        "0110",
                        noNotes,
                        noNotes,
                        noNotes,
                        "9961"+
                        "8873"+
                        "3471",
                        noNotes,
                        "4410"+
                        "6961"+
                        "4650",
                        noNotes
                    });
                case NotePosition.MiddleCenterRight:
                    return new Candidates(new String[] {
                        "1340"+
                        "1240"+
                        "0230",
                        noNotes,
                        noNotes,
                        noNotes,
                        "2992"+
                        "1991"+
                        "1661",
                        noNotes,
                        "3451"+
                        "9991"+
                        "3451",
                        noNotes
                    });
                case NotePosition.MiddleRight:
                    return new Candidates(new String[] {
                        "0350"+
                        "0551"+
                        "0551",
                        noNotes,
                        noNotes,
                        noNotes,
                        "2991"+
                        "2881"+
                        "0550",
                        noNotes,
                        "6661"+
                        "9972"+
                        "6661",
                        noNotes
                    });
                case NotePosition.BottomLeft:
                    return new Candidates(new String[] {
                        "3400"+
                        "2320"+
                        "0440",
                        noNotes,
                        noNotes,
                        noNotes,
                        "7740"+
                        "9980"+
                        "9920",
                        noNotes,
                        "4200"+
                        "5710"+
                        "9960",
                        noNotes
                    });
                case NotePosition.BottomCenterLeft:
                    return new Candidates(new String[] {
                        "1210"+
                        "4990"+
                        "5510",
                        noNotes,
                        noNotes,
                        noNotes,
                        "9970"+
                        "8860"+
                        "8840",
                        noNotes,
                        "2310"+
                        "7410"+
                        "9951",
                        noNotes
                    });
                case NotePosition.BottomCenterRight:
                    return new Candidates(new String[] {
                        "0970"+
                        "4600"+
                        "3540",
                        noNotes,
                        noNotes,
                        noNotes,
                        "2991"+
                        "1991"+
                        "4770",
                        noNotes,
                        "3661"+
                        "7731"+
                        "9991",
                        noNotes
                    });
                case NotePosition.BottomRight:
                    return new Candidates(new String[] {
                        "0230"+
                        "0550"+
                        "0330",
                        noNotes,
                        noNotes,
                        noNotes,
                        "1990"+
                        "1880"+
                        "0440",
                        noNotes,
                        "8940"+
                        "9940"+
                        "9951",
                        noNotes
                    });
            }
            return new Candidates();
        }

        private Candidates GetLeftCandidatesForDown(NotePosition position)
        {
            switch (position)
            {
                case NotePosition.TopLeft:
                    return new Candidates(new String[] {
                        "5420"+
                        "9940"+
                        "7720",
                        noNotes,
                        "3210"+
                        "6630"+
                        "4310",
                        noNotes,
                        noNotes,
                        noNotes,
                        "2200"+
                        "4400"+
                        "1000",
                        noNotes
                    });
                case NotePosition.TopCenterLeft:
                    return new Candidates(new String[] {
                        "4541"+
                        "7950"+
                        "4420",
                        noNotes,
                        "2320"+
                        "9640"+
                        "3320",
                        noNotes,
                        noNotes,
                        noNotes,
                        "6620"+
                        "7530"+
                        "3430",
                        noNotes
                    });
                case NotePosition.TopCenterRight:
                    return new Candidates(new String[] {
                        "6751"+
                        "7981"+
                        "3991",
                        noNotes,
                        "0530"+
                        "0641"+
                        "0751",
                        noNotes,
                        noNotes,
                        noNotes,
                        "4790"+
                        "2691"+
                        "0291",
                        noNotes
                    });
                case NotePosition.TopRight:
                    return new Candidates(new String[] {
                        "0661"+
                        "0761"+
                        "0441",
                        noNotes,
                        "0110"+
                        "0640"+
                        "0330",
                        noNotes,
                        noNotes,
                        noNotes,
                        "7765"+
                        "6620"+
                        "0220",
                        noNotes
                    });
                case NotePosition.MiddleLeft:
                    return new Candidates(new String[] {
                        "9720"+
                        "9820"+
                        "8720",
                        noNotes,
                        "7630"+
                        "7620"+
                        "3300",
                        noNotes,
                        noNotes,
                        noNotes,
                        "7310"+
                        "9820"+
                        "6510",
                        noNotes
                    });
                case NotePosition.MiddleCenterLeft:
                    return new Candidates(new String[] {
                        "8950"+
                        "9941"+
                        "5540",
                        noNotes,
                        "7620"+
                        "7600"+
                        "4210",
                        noNotes,
                        noNotes,
                        noNotes,
                        "8820"+
                        "9920"+
                        "9920",
                        noNotes
                    });
                case NotePosition.MiddleCenterRight:
                    return new Candidates(new String[] {
                        "8841"+
                        "9961"+
                        "5731",
                        noNotes,
                        "7750"+
                        "7620"+
                        "5520",
                        noNotes,
                        noNotes,
                        noNotes,
                        "9940"+
                        "9920"+
                        "4410",
                        noNotes
                    });
                case NotePosition.MiddleRight:
                    return new Candidates(new String[] {
                        "0970"+
                        "1970"+
                        "1440",
                        noNotes,
                        "0620"+
                        "0520"+
                        "0130",
                        noNotes,
                        noNotes,
                        noNotes,
                        "7733"+
                        "9966"+
                        "9960",
                        noNotes
                    });
                case NotePosition.BottomLeft:
                    return new Candidates(new String[] {
                        "9840"+
                        "9840"+
                        "9840",
                        noNotes,
                        "6410"+
                        "6410"+
                        "9710",
                        noNotes,
                        noNotes,
                        noNotes,
                        "4200"+
                        "9700"+
                        "9600",
                        noNotes
                    });
                case NotePosition.BottomCenterLeft:
                    return new Candidates(new String[] {
                        "9950"+
                        "9950"+
                        "7940",
                        noNotes,
                        "0000"+
                        "9930"+
                        "9920",
                        noNotes,
                        noNotes,
                        noNotes,
                        "4200"+
                        "9900"+
                        "9910",
                        noNotes
                    });
                case NotePosition.BottomCenterRight:
                    return new Candidates(new String[] {
                        "3970"+
                        "6940"+
                        "4991",
                        noNotes,
                        "1720"+
                        "3920"+
                        "5720",
                        noNotes,
                        noNotes,
                        noNotes,
                        "5630"+
                        "9931"+
                        "9971",
                        noNotes
                    });
                case NotePosition.BottomRight:
                    return new Candidates(new String[] {
                        "0190"+
                        "0370"+
                        "3560",
                        noNotes,
                        "0120"+
                        "0530"+
                        "0530",
                        noNotes,
                        noNotes,
                        noNotes,
                        "0220"+
                        "0520"+
                        "9993",
                        noNotes
                    });
            }
            return new Candidates();
        }

        private Candidates GetLeftCandidatesForLeft(NotePosition position)
        {
            switch (position)
            {
                case NotePosition.TopLeft:
                    return new Candidates(new String[] {
                        "0520"+
                        "7720"+
                        "3310",
                        noNotes,
                        "9941"+
                        "9960"+
                        "7620",
                        noNotes,
                        "9930"+
                        "9930"+
                        "8800",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.TopCenterLeft:
                    return new Candidates(new String[] {
                        "4420"+
                        "9920"+
                        "8820",
                        noNotes,
                        "5520"+
                        "9920"+
                        "7710",
                        noNotes,
                        "9910"+
                        "9910"+
                        "5510",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.TopCenterRight:
                    return new Candidates(new String[] {
                        "0310"+
                        "0210"+
                        "0110",
                        noNotes,
                        "9940"+
                        "8830"+
                        "5720",
                        noNotes,
                        "9950"+
                        "8850"+
                        "0430",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.TopRight:
                    return new Candidates(new String[] {
                        "0440"+
                        "0650"+
                        "0420",
                        noNotes,
                        "9930"+
                        "8820"+
                        "6610",
                        noNotes,
                        "0440"+
                        "1550"+
                        "0730",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.MiddleLeft:
                    return new Candidates(new String[] {
                        "5510"+
                        "7610"+
                        "4410",
                        noNotes,
                        "8760"+
                        "9950"+
                        "8760",
                        noNotes,
                        "9930"+
                        "9920"+
                        "6620",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.MiddleCenterLeft:
                    return new Candidates(new String[] {
                        "4431"+
                        "3321"+
                        "2210",
                        noNotes,
                        "9930"+
                        "9960"+
                        "9930",
                        noNotes,
                        "9910"+
                        "8810"+
                        "5510",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.MiddleCenterRight:
                    return new Candidates(new String[] {
                        "2320"+
                        "1110"+
                        "3310",
                        noNotes,
                        "8820"+
                        "9950"+
                        "8820",
                        noNotes,
                        "9920"+
                        "9910"+
                        "6610",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.MiddleRight:
                    return new Candidates(new String[] {
                        "2210"+
                        "2210"+
                        "3310",
                        noNotes,
                        "9940"+
                        "9940"+
                        "9940",
                        noNotes,
                        "1930"+
                        "1920"+
                        "1420",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.BottomLeft:
                    return new Candidates(new String[] {
                        "3300"+
                        "5500"+
                        "4400",
                        noNotes,
                        "6510"+
                        "9920"+
                        "7710",
                        noNotes,
                        "2210"+
                        "9910"+
                        "6610",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.BottomCenterLeft:
                    return new Candidates(new String[] {
                        "2210"+
                        "3310"+
                        "2210",
                        noNotes,
                        "6510"+
                        "9920"+
                        "9920",
                        noNotes,
                        "6610"+
                        "8810"+
                        "3210",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.BottomCenterRight:
                    return new Candidates(new String[] {
                        "3300"+
                        "5510"+
                        "4410",
                        noNotes,
                        "6730"+
                        "8920"+
                        "9950",
                        noNotes,
                        "2761"+
                        "4971"+
                        "3740",
                        noNotes,
                        noNotes,
                        noNotes
                    });
                case NotePosition.BottomRight:
                    return new Candidates(new String[] {
                        "0220"+
                        "0430"+
                        "0320",
                        noNotes,
                        "1620"+
                        "1930"+
                        "1930",
                        noNotes,
                        "0620"+
                        "0830"+
                        "0520",
                        noNotes,
                        noNotes,
                        noNotes
                    });
            }
            return new Candidates();
        }

        public Candidates GetRightCandidates(NoteDirection direction, NotePosition position)
        {
            NoteDirection flippedDirection;
            NotePosition flippedPosition;
            switch (direction)
            {
                case NoteDirection.Up:
                    flippedDirection = NoteDirection.Up;
                    break;
                case NoteDirection.UpRight:
                    flippedDirection = NoteDirection.UpLeft;
                    break;
                case NoteDirection.Right:
                    flippedDirection = NoteDirection.Left;
                    break;
                case NoteDirection.DownRight:
                    flippedDirection = NoteDirection.DownLeft;
                    break;
                case NoteDirection.Down:
                    flippedDirection = NoteDirection.Down;
                    break;
                case NoteDirection.DownLeft:
                    flippedDirection = NoteDirection.DownRight;
                    break;
                case NoteDirection.Left:
                    flippedDirection = NoteDirection.Right;
                    break;
                case NoteDirection.UpLeft:
                    flippedDirection = NoteDirection.UpRight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
            switch (position)
            {
                case NotePosition.TopLeft:
                    flippedPosition = NotePosition.TopRight;
                    break;
                case NotePosition.TopCenterLeft:
                    flippedPosition = NotePosition.TopCenterRight;
                    break;
                case NotePosition.TopCenterRight:
                    flippedPosition = NotePosition.TopCenterLeft;
                    break;
                case NotePosition.TopRight:
                    flippedPosition = NotePosition.TopLeft;
                    break;
                case NotePosition.MiddleLeft:
                    flippedPosition = NotePosition.MiddleRight;
                    break;
                case NotePosition.MiddleCenterLeft:
                    flippedPosition = NotePosition.MiddleCenterRight;
                    break;
                case NotePosition.MiddleCenterRight:
                    flippedPosition = NotePosition.MiddleCenterLeft;
                    break;
                case NotePosition.MiddleRight:
                    flippedPosition = NotePosition.MiddleLeft;
                    break;
                case NotePosition.BottomLeft:
                    flippedPosition = NotePosition.BottomRight;
                    break;
                case NotePosition.BottomCenterLeft:
                    flippedPosition = NotePosition.BottomCenterRight;
                    break;
                case NotePosition.BottomCenterRight:
                    flippedPosition = NotePosition.BottomCenterLeft;
                    break;
                case NotePosition.BottomRight:
                    flippedPosition = NotePosition.BottomLeft;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
            var leftCandidates = GetLeftCandidates(flippedDirection, flippedPosition);
            return leftCandidates.Flipped();
        }
    }
}
