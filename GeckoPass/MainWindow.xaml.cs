using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeckoPass
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public GexState GameState;

		public MainWindow()
		{
			InitializeComponent();
			GameState = new GexState();
		}

		public void SetStateUI(GexState state)
		{
			StartingPoint.SelectedIndex = (int)state.StartingWorld;
			LevelsCompleteBox.Text = state.LevelsComplete.ToString();
			XLevelsCompleteBox.Text = state.XLevelsComplete.ToString();

			ToonUnlocked.IsChecked = state.Unlockables.HasFlag(UnlockFlags.WorldMapToon);
			JungleUnlocked.IsChecked = state.Unlockables.HasFlag(UnlockFlags.WorldMapJungle);
			KungFuUnlocked.IsChecked = state.Unlockables.HasFlag(UnlockFlags.WorldMapKungFu);
			RezUnlocked.IsChecked = state.Unlockables.HasFlag(UnlockFlags.WorldMapRez);

			GraveBonus.IsChecked = state.Unlockables.HasFlag(UnlockFlags.BonusCompleteGrave);
			ToonBonus.IsChecked = state.Unlockables.HasFlag(UnlockFlags.BonusCompleteToon);
			JungleBonus.IsChecked = state.Unlockables.HasFlag(UnlockFlags.BonusCompleteJungle);
			KungFuBonus.IsChecked = state.Unlockables.HasFlag(UnlockFlags.BonusCompleteKungFu);
			RezBonus.IsChecked = state.Unlockables.HasFlag(UnlockFlags.BonusCompleteRez);

			KnockKnock.SelectedIndex = (int)state.KnockKnockState;

			UnusedBit.IsChecked = state.UnusedBit;

			ProtoBit.IsChecked = state.ProtoBit;

			ProtoUnk.Text = state.ProtoUnk.ToString();

		}

		public GexState GetStateFromUI()
		{
			GexState result = new GexState();

			result.StartingWorld = (GexWorlds)StartingPoint.SelectedIndex;

			if (!int.TryParse(LevelsCompleteBox.Text, out int levels))
				result.LevelsComplete = 0;
			else
				result.LevelsComplete = levels;

			if (!int.TryParse(XLevelsCompleteBox.Text, out int xlevels))
				result.XLevelsComplete = 0;
			else
				result.XLevelsComplete = xlevels;

			if ((bool)ToonUnlocked.IsChecked)
				result.Unlockables |= UnlockFlags.WorldMapToon;
			if ((bool)JungleUnlocked.IsChecked)
				result.Unlockables |= UnlockFlags.WorldMapJungle;
			if ((bool)KungFuUnlocked.IsChecked)
				result.Unlockables |= UnlockFlags.WorldMapKungFu;
			if ((bool)RezUnlocked.IsChecked)
				result.Unlockables |= UnlockFlags.WorldMapRez;

			if ((bool)GraveBonus.IsChecked)
				result.Unlockables |= UnlockFlags.BonusCompleteGrave;
			if ((bool)ToonBonus.IsChecked)
				result.Unlockables |= UnlockFlags.BonusCompleteToon;
			if ((bool)JungleBonus.IsChecked)
				result.Unlockables |= UnlockFlags.BonusCompleteJungle;
			if ((bool)KungFuBonus.IsChecked)
				result.Unlockables |= UnlockFlags.BonusCompleteKungFu;
			if ((bool)RezBonus.IsChecked)
				result.Unlockables |= UnlockFlags.BonusCompleteRez;

			result.KnockKnockState = (KnockState)KnockKnock.SelectedIndex;

			if ((bool)UnusedBit.IsChecked)
				result.UnusedBit = true;

			if ((bool)ProtoBit.IsChecked)
				result.ProtoBit = true;

			if (!int.TryParse(ProtoUnk.Text, out int unk))
				result.ProtoUnk = 0;
			else
				result.ProtoUnk = unk;

			return result;
		}

		private void ParsePass_Click(object sender, RoutedEventArgs e)
		{
			badPassword.Visibility = Visibility.Hidden;
			badLength.Visibility = Visibility.Hidden;
			badChecksum.Visibility = Visibility.Hidden;
			badNumber.Visibility = Visibility.Hidden;

			string noSpace = PasswordBox.Text.Replace(" ", "");
			PasswordBox.Text = noSpace;

			PasswordType type = (PasswordType)PassType.SelectedIndex;

			PasswordErrorType error = GexState.PasswordSanityCheck(noSpace, type);

			if (error == PasswordErrorType.InvalidCharacters)
			{
				badPassword.Visibility = Visibility.Visible;
				return;
			}
			else if (error == PasswordErrorType.InvalidLength)
			{
				badLength.Visibility = Visibility.Visible;
				return;
			}

			GexState state = new GexState();

			if (!state.LoadPassword(noSpace, type))
				badChecksum.Visibility = Visibility.Visible;

			SetStateUI(state);
		}

		private void MakePass_Click(object sender, RoutedEventArgs e)
		{
			badPassword.Visibility = Visibility.Hidden;
			badLength.Visibility = Visibility.Hidden;
			badChecksum.Visibility = Visibility.Hidden;
			badNumber.Visibility = Visibility.Hidden;

			var state = GetStateFromUI();

			if (state.LevelsComplete < 0 ||
				state.XLevelsComplete < 0 ||
				state.ProtoUnk < 0)
			{
				badNumber.Visibility = Visibility.Visible;
				PasswordBox.Text = "";
				return;
			}

			PasswordType type = (PasswordType)PassType.SelectedIndex;

			PasswordBox.Text = state.WritePassword(type);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			StartingPoint.SelectedIndex = 6;
			LevelsCompleteBox.Text = "19";
			XLevelsCompleteBox.Text = "8";

			ToonUnlocked.IsChecked = true;
			JungleUnlocked.IsChecked = true;
			KungFuUnlocked.IsChecked = true;
			RezUnlocked.IsChecked = true;

			GraveBonus.IsChecked = true;
			ToonBonus.IsChecked = true;
			JungleBonus.IsChecked = true;
			KungFuBonus.IsChecked = true;
			RezBonus.IsChecked = true;

			KnockKnock.SelectedIndex = 1;
		}
	}
}