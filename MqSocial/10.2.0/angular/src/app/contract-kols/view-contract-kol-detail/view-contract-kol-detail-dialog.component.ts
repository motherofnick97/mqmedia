import { Component, EventEmitter, Injector, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/app-component-base';
import {
    ContractKolDto,
    ContractKolServiceProxy,
    ContractKolStatus,
    ReceiveStatus,
} from '@shared/service-proxies/service-proxies';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AbpModalHeaderComponent } from '../../../shared/components/modal/abp-modal-header.component';
import { LocalizePipe } from '@shared/pipes/localize.pipe';
import { Tag } from 'primeng/tag';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Textarea } from 'primeng/textarea';
import { DatePicker } from 'primeng/datepicker';
import moment from 'moment';

@Component({
    templateUrl: './view-contract-kol-detail-dialog.component.html',
    standalone: true,
    imports: [
        FormsModule,
        AbpModalHeaderComponent,
        LocalizePipe,
        DatePipe,
        DecimalPipe,
        Tag,
        Button,
        Select,
        InputText,
        InputNumber,
        Textarea,
        DatePicker,
    ],
})
export class ViewContractKolDetailDialogComponent extends AppComponentBase {
    @Output() onSave = new EventEmitter<any>();

    record: ContractKolDto = new ContractKolDto();
    kolName: string = '—';
    contractName: string = '—';
    saving = false;

    airTimeDate: Date | null = null;

    readonly statusOptions = [
        { value: ContractKolStatus.Register, label: 'Đăng ký' },
        { value: ContractKolStatus.Approve, label: 'Duyệt đăng ký' },
        { value: ContractKolStatus.Processing, label: 'Đang tiến hành' },
        { value: ContractKolStatus.MktOk, label: 'Mkt duyệt' },
        { value: ContractKolStatus.DpmOk, label: 'Quản lý duyệt' },
        { value: ContractKolStatus.OnAir, label: 'Đã air' },
        { value: ContractKolStatus.Following, label: 'Theo dõi' },
        { value: ContractKolStatus.Paid, label: 'Đã thanh toán' },
        { value: ContractKolStatus.Done, label: 'Hoàn thành' },
        { value: ContractKolStatus.Cancel, label: 'Hủy' },
        { value: ContractKolStatus.Reject, label: 'Từ chối' },
    ];

    readonly receiveStatusOptions = [
        { value: ReceiveStatus.NotShip, label: 'Chưa gửi' },
        { value: ReceiveStatus.Shipping, label: 'Đang gửi' },
        { value: ReceiveStatus.Received, label: 'Đã nhận' },
        { value: ReceiveStatus.NotReceived, label: 'Không nhận' },
    ];

    readonly statusSeverities: Record<number, 'secondary' | 'info' | 'success' | 'warn' | 'danger'> = {
        [ContractKolStatus.Register]: 'secondary',
        [ContractKolStatus.Approve]: 'info',
        [ContractKolStatus.Processing]: 'info',
        [ContractKolStatus.MktOk]: 'warn',
        [ContractKolStatus.DpmOk]: 'warn',
        [ContractKolStatus.OnAir]: 'success',
        [ContractKolStatus.Following]: 'secondary',
        [ContractKolStatus.Paid]: 'success',
        [ContractKolStatus.Done]: 'success',
        [ContractKolStatus.Cancel]: 'danger',
        [ContractKolStatus.Reject]: 'danger',
    };

    readonly receiveStatusSeverities: Record<number, 'secondary' | 'info' | 'success' | 'danger'> = {
        [ReceiveStatus.NotShip]: 'secondary',
        [ReceiveStatus.Shipping]: 'info',
        [ReceiveStatus.Received]: 'success',
        [ReceiveStatus.NotReceived]: 'danger',
    };

    constructor(
        injector: Injector,
        public bsModalRef: BsModalRef,
        private _contractKolService: ContractKolServiceProxy
    ) {
        super(injector);
    }

    setRecord(record: ContractKolDto): void {
        this.record = ContractKolDto.fromJS(record.toJSON());
        this.airTimeDate = record.airTime ? record.airTime.toDate() : null;
    }

    getStatusSeverity(status: ContractKolStatus | undefined): 'secondary' | 'info' | 'success' | 'warn' | 'danger' {
        return status != null ? (this.statusSeverities[status] ?? 'secondary') : 'secondary';
    }

    getReceiveStatusSeverity(s: ReceiveStatus | undefined): 'secondary' | 'info' | 'success' | 'danger' {
        return s != null ? (this.receiveStatusSeverities[s] ?? 'secondary') : 'secondary';
    }

    save(): void {
        this.saving = true;
        this.record.airTime = this.airTimeDate ? moment(this.airTimeDate) : undefined;

        this._contractKolService.update(this.record).subscribe({
            next: () => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.bsModalRef.hide();
                this.onSave.emit();
            },
            error: () => { this.saving = false; },
        });
    }
}
