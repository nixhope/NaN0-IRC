using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaN0IRC
{
    class RPSLS
    {
        String reply = "";
        int winStreak = 0;
        // Play a game, given the player's choice. This must be valid.
        public string play(string input)
        {
            Choice playerChoice = this.getPlayerChoice(input);
            Choice computerChoice = this.getComputerChoice();
            Result result = this.evaluateChoice(playerChoice, computerChoice);
            switch (result)
            {
                case Result.Win:
                    reply += ", you win!";
                    winStreak++;
                    break;
                case Result.Loss:
                    reply += ", you lose!";
                    winStreak = 0;
                    break;
                default: // Draw
                    reply += ", a draw, how dull.";
                    break;
            }
            return reply;
        }

        private Choice getPlayerChoice(string input)
        {
            Choice pChoice = Choice.Rock;
            for (int i = 0; i < 5; i++)
                if (((Choice)i).ToString().ToLower() == input.ToLower())
                    pChoice = (Choice)i;
            reply = "You picked " + pChoice.ToString();
            return pChoice;
        }

        Choice getComputerChoice()
        {
            Random random = new Random();
            Choice pcChoice = (Choice)(random.Next(5));
            reply += " and I picked " + pcChoice.ToString() + ". ";
            return pcChoice;
        }

        // Evaluates win
        Result evaluateChoice(Choice player, Choice computer)
        {
            Result result;
            // Find out who wins
            // There has to be a more elegant way of doing this!
            switch (player)
            {
                case Choice.Rock:
                    switch (computer)
                    {
                        case Choice.Rock:
                            reply += "Rock rubs Rock";
                            result = Result.Draw;
                            break;
                        case Choice.Paper:
                            reply += "Paper covers Rock";
                            result = Result.Loss;
                            break;
                        case Choice.Scissors:
                            reply += "Rock breaks Scissors";
                            result = Result.Win;
                            break;
                        case Choice.Lizard:
                            reply += "Rock crushes Lizard";
                            result = Result.Win;
                            break;
                        default: // Spock
                            reply += "Spock vaporises Rock";
                            result = Result.Loss;
                            break;
                    }
                    break;
                case Choice.Paper:
                    switch (computer)
                    {
                        case Choice.Rock:
                            reply += "Paper covers Rock";
                            result = Result.Win;
                            break;
                        case Choice.Paper:
                            reply += "Paper plies Paper";
                            result = Result.Draw;
                            break;
                        case Choice.Scissors:
                            reply += "Scissors cuts Paper";
                            result = Result.Loss;
                            break;
                        case Choice.Lizard:
                            reply += "Lizard eats Paper";
                            result = Result.Loss;
                            break;
                        default: // Spock
                            reply += "Paper disproves Spock";
                            result = Result.Win;
                            break;
                    }
                    break;
                case Choice.Scissors:
                    switch (computer)
                    {
                        case Choice.Rock:
                            reply += "Rock breaks Scissors";
                            result = Result.Loss;
                            break;
                        case Choice.Paper:
                            reply += "Scissors cuts Paper";
                            result = Result.Win;
                            break;
                        case Choice.Scissors:
                            reply += "Scissors snubs Scissors";
                            result = Result.Draw;
                            break;
                        case Choice.Lizard:
                            reply += "Scissors decapitates Lizard";
                            result = Result.Win;
                            break;
                        default: // Spock
                            reply += "Spock smashes Scissors";
                            result = Result.Loss;
                            break;
                    }
                    break;
                case Choice.Lizard:
                    switch (computer)
                    {
                        case Choice.Rock:
                            reply += "Rock crushes Lizard";
                            result = Result.Loss;
                            break;
                        case Choice.Paper:
                            reply += "Lizard eats Paper";
                            result = Result.Win;
                            break;
                        case Choice.Scissors:
                            reply += "Scissors decapitates Lizard";
                            result = Result.Loss;
                            break;
                        case Choice.Lizard:
                            reply += "Lizard licks Lizard";
                            result = Result.Draw;
                            break;
                        default: // Spock
                            reply += "Lizard Poisons Spock";
                            result = Result.Win;
                            break;
                    }
                    break;
                default: // Spock
                    switch (computer)
                    {
                        case Choice.Rock:
                            reply += "Spock vaporises Rock";
                            result = Result.Win;
                            break;
                        case Choice.Paper:
                            reply += "Paper disproves Spock";
                            result = Result.Loss;
                            break;
                        case Choice.Scissors:
                            reply += "Spock smashes Scissors";
                            result = Result.Win;
                            break;
                        case Choice.Lizard:
                            reply += "Lizard Poisons Spock";
                            result = Result.Loss;
                            break;
                        default: // Spock
                            reply += "Spock salutes Spock";
                            result = Result.Draw;
                            break;
                    }
                    break;
            }
            return result;
        }

        private enum Choice
        {
            Rock,
            Paper,
            Scissors,
            Lizard,
            Spock,
        }

        private enum Result
        {
            Win = 0,
            Loss = 1,
            Draw = 2,
        }

        #region Constructor
        public RPSLS() { }
        #endregion Constructor
    }
}
