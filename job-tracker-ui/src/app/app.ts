import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { environment } from '../environments/environment';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly auth = inject(AuthService);

  protected readonly swaggerUrl = environment.swaggerUrl;
  protected readonly repoUrl = environment.repoUrl;
  protected readonly portfolioUrl = environment.portfolioUrl;

  protected logout(): void {
    this.auth.logout();
  }
}