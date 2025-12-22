import { ExclamationCircleIcon } from '@heroicons/react/24/outline';

interface ErrorAlertProps {
  message: string;
  onDismiss?: () => void;
}

export const ErrorAlert = ({ message, onDismiss }: ErrorAlertProps) => {
  return (
    <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded-lg flex items-center justify-between">
      <div className="flex items-center">
        <ExclamationCircleIcon className="h-5 w-5 mr-2" />
        <span>{message}</span>
      </div>
      {onDismiss && (
        <button
          onClick={onDismiss}
          className="ml-4 text-red-600 hover:text-red-800"
        >
          ×
        </button>
      )}
    </div>
  );
};


