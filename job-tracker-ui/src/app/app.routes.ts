import { Routes } from '@angular/router';
import { ApplicationList } from './components/application-list/application-list';
import { ApplicationForm } from './components/application-form/application-form';
import { Login } from './components/login/login';
import { adminGuard, authGuard, guestGuard } from './guards/auth.guards';

export const routes: Routes = [
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: '', component: ApplicationList, canActivate: [authGuard] },
  { path: 'add', component: ApplicationForm, canActivate: [adminGuard] },
  { path: 'edit/:id', component: ApplicationForm, canActivate: [adminGuard] },
  { path: '**', redirectTo: '' }
];