import { BehaviorSubject } from 'rxjs';
import { Service } from 'typedi';

// Create a discriminated union type for the log state. The base type will have a text and timestamp property.
// The discriminated union will have a type property that can be either 'log' or 'error'.

type MessageBase = {
  text: string;
  timestamp: number;
};

export type LogMessage = MessageBase & {
  type: 'log';
};

export type ErrorMessage = MessageBase & {
  type: 'error';
};

export type Message = LogMessage | ErrorMessage;

@Service({
  global: true,
})
export default class LogState {
  public Messages = new BehaviorSubject<Message[]>([]);

  public log(message: string) {
    const logMessage: LogMessage = {
      type: 'log',
      text: message,
      timestamp: Date.now(),
    };
    this.Messages.next([...this.Messages.value, logMessage]);
  }

  public logMany(...messages: string[]) {
    const logMessages: LogMessage[] = messages.map((message) => ({
      type: 'log',
      text: message,
      timestamp: Date.now(),
    }));
    this.Messages.next([...this.Messages.value, ...logMessages]);
  }

  public error(message: string) {
    const errorMessage: ErrorMessage = {
      type: 'error',
      text: message,
      timestamp: Date.now(),
    };
    this.Messages.next([...this.Messages.value, errorMessage]);
  }

  public errorMany(...messages: string[]) {
    const errorMessages: ErrorMessage[] = messages.map((message) => ({
      type: 'error',
      text: message,
      timestamp: Date.now(),
    }));
    this.Messages.next([...this.Messages.value, ...errorMessages]);
  }
}
