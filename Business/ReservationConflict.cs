using OfficeBooking.Models;

namespace OfficeBooking.Business
{
    public static class ReservationConflict
    {
        // Standardowa logika nakładania się przedziałów czasu:
        public static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
            => startA < endB && endA > startB;

        // Sprawdza czy nowa rezerwacja koliduje z istniejącymi w tej samej sali
        public static bool HasConflict(
            IEnumerable<Reservation> existingReservations,
            DateTime newStart,
            DateTime newEnd,
            int? ignoreReservationId = null)
        {
            foreach (var r in existingReservations)
            {
                if (r.IsCancelled) continue;
                if (ignoreReservationId.HasValue && r.Id == ignoreReservationId.Value) continue;

                if (Overlaps(newStart, newEnd, r.Start, r.End))
                    return true;
            }

            return false;
        }
    }
}

