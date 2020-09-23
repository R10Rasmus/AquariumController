using Lcd1602Controller;

namespace AquariumController.Display
{
    public class SetCharacters
    {
        public const int TemperatureCharactersNumber = 6;

        public static void TemperatureCharacters(Lcd1602 lcd)
        {
            byte[] temperatureCharacters = new byte[8];
            temperatureCharacters[0] = 0b_11000;
            temperatureCharacters[1] = 0b_11000;
            temperatureCharacters[2] = 0b_00011;
            temperatureCharacters[3] = 0b_00100;
            temperatureCharacters[4] = 0b_00100;
            temperatureCharacters[5] = 0b_00100;
            temperatureCharacters[6] = 0b_00011;
            temperatureCharacters[7] = 0b_00000;

            lcd.CreateCustomCharacter(TemperatureCharactersNumber, temperatureCharacters);
        }

        public static void FishCharacters(Lcd1602 lcd)
        {
            byte[] Fish1 = new byte[8];
            Fish1[0] = 0b_00000;
            Fish1[1] = 0b_00000;
            Fish1[2] = 0b_00000;
            Fish1[3] = 0b_00000;
            Fish1[4] = 0b_00000;
            Fish1[5] = 0b_10110;
            Fish1[6] = 0b_11001;
            Fish1[7] = 0b_10110;

            byte[] Fish2 = new byte[8];
            Fish2[0] = 0b_00000;
            Fish2[1] = 0b_00000;
            Fish2[2] = 0b_00000;
            Fish2[3] = 0b_00000;
            Fish2[4] = 0b_10110;
            Fish2[5] = 0b_11001;
            Fish2[6] = 0b_00110;
            Fish2[7] = 0b_00000;

            byte[] Fish3 = new byte[8];
            Fish3[0] = 0b_00000;
            Fish3[1] = 0b_00000;
            Fish3[2] = 0b_00000;
            Fish3[3] = 0b_00110;
            Fish3[4] = 0b_11001;
            Fish3[5] = 0b_10110;
            Fish3[6] = 0b_00000;
            Fish3[7] = 0b_00000;
            byte[] Fish4 = new byte[8];
            Fish4[0] = 0b_00000;
            Fish4[1] = 0b_00000;
            Fish4[2] = 0b_10110;
            Fish4[3] = 0b_11001;
            Fish4[4] = 0b_10110;
            Fish4[5] = 0b_00000;
            Fish4[6] = 0b_00000;
            Fish4[7] = 0b_00000;

            byte[] Fish5 = new byte[8];
            Fish5[0] = 0b_00000;
            Fish5[1] = 0b_00110;
            Fish5[2] = 0b_11001;
            Fish5[3] = 0b_10110;
            Fish5[4] = 0b_00000;
            Fish5[5] = 0b_00000;
            Fish5[6] = 0b_00000;
            Fish5[7] = 0b_00000;

            byte[] Fish6 = new byte[8];
            Fish6[0] = 0b_10110;
            Fish6[1] = 0b_11001;
            Fish6[2] = 0b_00110;
            Fish6[3] = 0b_00000;
            Fish6[4] = 0b_00000;
            Fish6[5] = 0b_00000;
            Fish6[6] = 0b_00000;
            Fish6[7] = 0b_00000;

            lcd.CreateCustomCharacter(0, Fish1);
            lcd.CreateCustomCharacter(1, Fish2);
            lcd.CreateCustomCharacter(2, Fish3);
            lcd.CreateCustomCharacter(3, Fish4);
            lcd.CreateCustomCharacter(4, Fish5);
            lcd.CreateCustomCharacter(5, Fish6);
        }
    }
}
