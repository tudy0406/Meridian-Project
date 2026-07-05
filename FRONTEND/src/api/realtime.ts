import * as signalR from '@microsoft/signalr';
import { HUB_URL, TOKEN_STORAGE_KEY } from '../config';

/**
 * Thin wrapper around the SignalR connection to the notifications hub. Consumers
 * subscribe to server-pushed events (new notifications, task/progress changes)
 * — this is the client half of the Observer pattern.
 */
export class RealtimeConnection {
  private connection: signalR.HubConnection | null = null;

  start() {
    if (this.connection) return this.connection;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => localStorage.getItem(TOKEN_STORAGE_KEY) ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.start().catch((err) => console.error('SignalR connection error', err));
    return this.connection;
  }

  on(event: string, handler: (...args: unknown[]) => void) {
    this.connection?.on(event, handler);
  }

  off(event: string, handler: (...args: unknown[]) => void) {
    this.connection?.off(event, handler);
  }

  async stop() {
    await this.connection?.stop();
    this.connection = null;
  }
}

export const realtime = new RealtimeConnection();

export const RealtimeEvents = {
  NotificationReceived: 'notificationReceived',
  TasksChanged: 'tasksChanged',
  ProgressChanged: 'progressChanged',
} as const;
