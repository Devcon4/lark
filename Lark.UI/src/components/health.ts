import { LitElement, html } from 'lit-element';
import { customElement } from 'lit/decorators.js';

@customElement('health-component')
export class HealthComponent extends LitElement {
  render() {
    return html`<div class="health">100</div>`;
  }
}
