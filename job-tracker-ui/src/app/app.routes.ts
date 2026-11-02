import { Routes } from '@angular/router';
import { ApplicationList } from './components/application-list/application-list';
import { ApplicationForm } from './components/application-form/application-form';

export const routes: Routes = [
  { path: '', component: ApplicationList },
  { path: 'add', component: ApplicationForm },
  { path: 'edit/:id', component: ApplicationForm },
  { path: '**', redirectTo: '' }
];