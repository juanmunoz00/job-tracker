export type ApplicationStatus =
  | 'Applied'
  | 'PhoneScreen'
  | 'Interview'
  | 'TakeHome'
  | 'Offer'
  | 'Rejected'
  | 'Withdrawn';

export interface Application {
  id: number;
  company: string;
  country: string;
  position: string;
  appliedDate: string;   // "2026-10-20" — JSON has no date type, so dates arrive as text
  status: ApplicationStatus;
  statusDate: string;    // "2026-10-25T21:30:00Z"
}

export interface CreateApplication {
  company: string;
  country: string;
  position: string;
  appliedDate: string;
}