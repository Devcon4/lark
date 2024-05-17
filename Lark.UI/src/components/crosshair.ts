import { css } from 'lit';
import { LitElement, html } from 'lit-element';
import { customElement } from 'lit/decorators.js';

@customElement('crosshair-component')
export default class CrosshairComponent extends LitElement {
  render() {
    return html`<svg class="crosshair" viewBox="0 0 50 50">
      <circle cx="25" cy="25" r="3" fill="red" />
    </svg>`;
  }

  static styles = css`
    :host {
      flex: 1;
      display: flex;
      justify-content: center;
    }

    .crosshair {
      width: 50px;
      height: 50px;
    }
  `;
}
