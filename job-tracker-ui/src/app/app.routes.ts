import { Routes } from '@angular/router';
import { ApplicationList } from './components/application-list/application-list';

export const routes: Routes = [
  { path: '', component: ApplicationList },
  { path: '**', redirectTo: '' }
];