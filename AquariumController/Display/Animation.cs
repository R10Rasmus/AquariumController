using Lcd1602Controller;
using System.Threading;

namespace AquariumController.Display
{
    public static class Animation
    {
        public static void ShowFishOnLine2(LcdConsole console, ref int _fishCount, ref bool _revers, ref int _positionCount)
        {
            string tmp = ((char)_fishCount).ToString();
            for (int i = 0; i < _positionCount; i++)
            {
                tmp = " " + tmp;
            }
            console.ReplaceLine(1, tmp);

            if (_fishCount == 5)
            {
                _revers = true;
            }
            if (_fishCount == 0)
            {
                _revers = false;
            }

            if (!_revers)
            {
                _fishCount++;
            }
            else
            {
                _fishCount--;
            }

            if (_positionCount < 15)
            {
                _positionCount++;
            }
            else
            {
                _positionCount = 0;
            }
        }
    }
}
