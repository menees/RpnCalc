namespace Menees.RpnCalc.Internal
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;

	#endregion

	internal class DateTimeCommands : Commands
	{
		#region Constructors

		public DateTimeCommands(Calculator calc)
			: base(calc)
		{
		}

		#endregion

		#region Private Properties

#pragma warning disable MEN013 // Use UTC time. This needs to be the local date time.
		private static DateTime LocalNow => DateTime.Now;
#pragma warning restore MEN013 // Use UTC time

		#endregion

		#region Public Methods

		public static void Date(Command cmd)
		{
			cmd.Commit(new DateTimeValue(LocalNow.Date));
		}

		public static void Time(Command cmd)
		{
			cmd.Commit(GetTimePart(LocalNow));
		}

		public static void Now(Command cmd)
		{
			cmd.Commit(new DateTimeValue(LocalNow));
		}

		public void AgeOn(Command cmd)
		{
			this.RequireArgs(2);
			this.RequireType(0, RpnValueType.DateTime);
			this.RequireType(1, RpnValueType.DateTime);
			var values = cmd.UseTopValues(2);
			var birthDate = (DateTimeValue)values[1];
			var onDate = (DateTimeValue)values[0];
			cmd.Commit(CalculateAge(birthDate.AsDateTime, onDate.AsDateTime));
		}

		public void AgeToday(Command cmd)
		{
			var birthDate = this.UseTopDateTime(cmd);
			cmd.Commit(CalculateAge(birthDate.AsDateTime, LocalNow.Date));
		}

		public void DatePart(Command cmd)
		{
			var value = this.UseTopDateTime(cmd);
			cmd.Commit(new DateTimeValue(value.AsDateTime.Date));
		}

		public void DayOfYear(Command cmd)
		{
			var value = this.UseTopDateTime(cmd);

			// This returns a 1-based value already, so we don't need to offset it.
			cmd.Commit(new IntegerValue(value.AsDateTime.DayOfYear));
		}

		public void TimePart(Command cmd)
		{
			var value = this.UseTopDateTime(cmd);
			cmd.Commit(GetTimePart(value.AsDateTime));
		}

		public void Weekday(Command cmd)
		{
			var value = this.UseTopDateTime(cmd);

			// Sunday is 0, Monday is 1, etc.  Make it 1-based for the user since DayOfYear is.
			cmd.Commit(new IntegerValue(((int)value.AsDateTime.DayOfWeek) + 1));
		}

		#endregion

		#region Private Methods

		private static TimeSpanValue GetTimePart(DateTime value)
		{
			// I originally returned a DateTimeValue from: new DateTime(value.TimeOfDay.Ticks).
			// However, that produced a weird display value if you asked for the TimePart of a
			// date-only value.  That returned the "0 date", which we displayed as 1/1/0001.
			// So I didn't like TimePart seemingly returning a date.  I ultimately decided to do
			// what .NET's DateTime.TimeOfDay property does and just return a TimeSpan.
			// You lose the AM/PM formatting, but a time without a date really is a TimeSpan.
			TimeSpanValue result = new(value.TimeOfDay);
			return result;
		}

		private static IntegerValue CalculateAge(DateTime birthDate, DateTime onDate)
		{
			int multiplier = 1;
			if (birthDate > onDate)
			{
				DateTime temp = birthDate;
				birthDate = onDate;
				onDate = temp;
				multiplier = -1;
			}

			// Get the difference in years.
			int years = onDate.Year - birthDate.Year;

			// Subtract a year if onDate's MM/DD is before birthDate's MM/DD.
			if ((onDate.Month < birthDate.Month) ||
				(onDate.Month == birthDate.Month && onDate.Day < birthDate.Day))
			{
				years--;
			}

			return new IntegerValue(multiplier * years);
		}

		private DateTimeValue UseTopDateTime(Command cmd)
		{
			this.RequireArgs(1);
			this.RequireType(0, RpnValueType.DateTime);
			var result = (DateTimeValue)cmd.UseTopValue();
			return result;
		}

		#endregion
	}
}
