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
  appliedDate: string;
  status: ApplicationStatus;
  statusDate: string;
}

export interface CreateApplication {
  company: string;
  country: string;
  position: string;
  appliedDate: string;
}

export const STATUSES: ApplicationStatus[] = [
  'Applied',
  'PhoneScreen',
  'Interview',
  'TakeHome',
  'Offer',
  'Rejected',
  'Withdrawn'
];

export const STATUS_LABELS: Record<ApplicationStatus, string> = {
  Applied: 'Applied',
  PhoneScreen: 'Phone screen',
  Interview: 'Interview',
  TakeHome: 'Take-home',
  Offer: 'Offer',
  Rejected: 'Rejected',
  Withdrawn: 'Withdrawn'
};