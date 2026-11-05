import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

interface DemoAccount {
  username: string;
  password: string;
  role: string;
  description: string;
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly demoAccounts: DemoAccount[] = [
    { username: 'admin', password: 'Admin123!', role: 'Admin', description: 'Add, edit and delete applications' },
    { username: 'demo', password: 'Demo123!', role: 'Read-only', description: 'Browse, search and filter' }
  ];

  protected readonly expired = this.route.snapshot.queryParamMap.get('reason') === 'expired';
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showPassword = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    username: ['', Validators.required],
    password: ['', Validators.required]
  });

  protected useAccount(account: DemoAccount): void {
    this.form.setValue({ username: account.username, password: account.password });
    this.error.set(null);
  }

  protected togglePassword(): void {
    this.showPassword.update(shown => !shown);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err: HttpErrorResponse) => {
        this.error.set(err.status === 401
          ? 'Invalid username or password.'
          : 'Could not reach the server. It may be waking up, so try again in a few seconds.');
        this.submitting.set(false);
      }
    });
  }
}